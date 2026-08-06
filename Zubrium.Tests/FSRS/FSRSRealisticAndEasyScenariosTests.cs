using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;
using Zubrium.Domain;
using Zubrium.SpacedRepetition;
using FSRS.Core.Enums;
using FSRS.Core.Configurations;
using FSRS.Core.Services;

namespace Zubrium.Tests.FSRS
{
    public class FSRSRealisticAndEasyScenariosTests
    {
        private readonly ITestOutputHelper _output;

        public FSRSRealisticAndEasyScenariosTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private Card CreateNewCard()
        {
            return new Card(
                id: Guid.NewGuid().ToString("N"),
                frontMarkdown: "Front",
                briefMarkdown: "Brief"
            );
        }

        // ====================================================================
        // 11. Реалистичные и смешанные паттерны пользователя
        // ====================================================================

        [Fact]
        public void TC_11_1_RareLapseOnStableMaterial()
        {
            var fsrs = new FSRSService();
            var card = CreateNewCard();
            var now = DateTime.UtcNow;

            // 1. Стабильный материал (5x Good)
            for (int i = 0; i < 5; i++)
            {
                fsrs.ApplyRating(card, Rating.Good, now);
                now = now.AddDays(Math.Max(1, card.ScheduledDays));
            }
            var stabilityBeforeLapse = card.Stability.Value;
            var intervalBeforeLapse = card.ScheduledDays;
            _output.WriteLine($"[TC_11_1] Before Lapse -> S: {stabilityBeforeLapse:F4}, Interval: {intervalBeforeLapse} days");

            // 2. Редкая ошибка (Случайный Lapse)
            fsrs.ApplyRating(card, Rating.Again, now);
            var stabilityAfterLapse = card.Stability.Value;
            _output.WriteLine($"[TC_11_1] After 'Again' -> S: {stabilityAfterLapse:F4}, Interval: {card.ScheduledDays} days");

            // 3. Восстановление (2x Good)
            now = now.AddMinutes(10);
            fsrs.ApplyRating(card, Rating.Good, now); // Выход из Relearning
            now = now.AddDays(Math.Max(1, card.ScheduledDays));
            fsrs.ApplyRating(card, Rating.Good, now);

            _output.WriteLine($"[TC_11_1] After 2x 'Good' Rebound -> S: {card.Stability:F4}, Interval: {card.ScheduledDays} days");

            Assert.True(stabilityAfterLapse < stabilityBeforeLapse);
            Assert.True(card.ScheduledDays > 2, "Interval should rebound quickly due to rich history");
        }

        [Fact]
        public void TC_11_2_ToughStart_ThenSmoothSailing()
        {
            var fsrs = new FSRSService();
            var card = CreateNewCard();
            var now = DateTime.UtcNow;

            // Трудный старт
            Rating[] badStart = { Rating.Again, Rating.Again, Rating.Hard };
            foreach (var r in badStart)
            {
                fsrs.ApplyRating(card, r, now);
                now = now.AddMinutes(10);
            }

            var difficultyAfterBadStart = card.Difficulty.Value;
            _output.WriteLine($"[TC_11_2] D after bad start: {difficultyAfterBadStart:F4}");

            // Уверенное плавание
            for (int i = 0; i < 4; i++)
            {
                now = now.AddDays(Math.Max(1, card.ScheduledDays));
                fsrs.ApplyRating(card, Rating.Good, now);
            }

            _output.WriteLine($"[TC_11_2] Final D: {card.Difficulty:F4}, Final Interval: {card.ScheduledDays} days");

            Assert.True(difficultyAfterBadStart > 7.0, "Difficulty should be very high initially");
            Assert.True(card.Difficulty.Value < difficultyAfterBadStart, "Difficulty should drop after consecutive Goods");
            Assert.True(card.ScheduledDays >= 7, "Interval should eventually reach weeks");
        }

        [Fact]
        public void TC_11_3_GradualFading()
        {
            var fsrs = new FSRSService();
            var card = CreateNewCard();
            var now = DateTime.UtcNow;

            // 3x Good
            for (int i = 0; i < 3; i++)
            {
                fsrs.ApplyRating(card, Rating.Good, now);
                now = now.AddDays(Math.Max(1, card.ScheduledDays));
            }

            var sAfterGood = card.Stability.Value;
            _output.WriteLine($"[TC_11_3] Stability after Goods: {sAfterGood:F4}");

            // 2x Hard
            fsrs.ApplyRating(card, Rating.Hard, now);
            now = now.AddDays(Math.Max(1, card.ScheduledDays));
            fsrs.ApplyRating(card, Rating.Hard, now);

            var sAfterHards = card.Stability.Value;
            _output.WriteLine($"[TC_11_3] Stability after 2 Hards: {sAfterHards:F4}");
            Assert.True(sAfterHards > sAfterGood, "Stability still grows on Hard, but slowly");

            // Finally Again
            now = now.AddDays(Math.Max(1, card.ScheduledDays));
            fsrs.ApplyRating(card, Rating.Again, now);

            _output.WriteLine($"[TC_11_3] Final State after Again: {card.State}, Interval: {card.ScheduledDays}");
            Assert.Equal(3, card.State); // 3 = Relearning
        }

        [Fact]
        public void TC_11_4_EternalHard()
        {
            var fsrs = new FSRSService();
            var card = CreateNewCard();
            var now = DateTime.UtcNow;

            // Выводим карточку из состояния "New/Learning" в уверенное "Review", 
            // иначе Hard будет удерживать её в минутных интервалах (Interval = 0)
            while (card.State != 2 || card.ScheduledDays == 0) // 2 = Review
            {
                fsrs.ApplyRating(card, Rating.Good, now);
                // Если интервал 0, двигаем время на 10 минут, иначе на ScheduledDays
                now = card.ScheduledDays == 0 ? now.AddMinutes(10) : now.AddDays(card.ScheduledDays);
            }

            var startInterval = card.ScheduledDays;
            _output.WriteLine($"[TC_11_4] Base Interval before Hards: {startInterval} days");

            for (int i = 1; i <= 10; i++)
            {
                fsrs.ApplyRating(card, Rating.Hard, now);
                now = now.AddDays(Math.Max(1, card.ScheduledDays));

                if (i % 2 == 0)
                {
                    _output.WriteLine($"[TC_11_4] Step {i}: D = {card.Difficulty:F4}, Interval = {card.ScheduledDays}");
                }
            }

            Assert.InRange(card.Difficulty.Value, 8.0, 10.0);
            Assert.True(card.ScheduledDays >= startInterval, "Interval should slowly grow or at least hold, not collapse.");
        }

        [Fact]
        public void TC_11_5_NervousStudent_EarlyGoods()
        {
            var fsrs = new FSRSService();
            var card = CreateNewCard();
            var now = DateTime.UtcNow;

            while (card.ScheduledDays < 25 && card.Reps < 10)
            {
                fsrs.ApplyRating(card, Rating.Good, now);
                now = now.AddDays(Math.Max(1, card.ScheduledDays));
            }

            var baseInterval = card.ScheduledDays;
            _output.WriteLine($"[TC_11_5] Base Interval before spam: {baseInterval} days");

            for (int i = 0; i < 5; i++)
            {
                now = now.AddDays(2);
                fsrs.ApplyRating(card, Rating.Good, now);
            }

            _output.WriteLine($"[TC_11_5] Final Interval after 5 early 'Goods': {card.ScheduledDays} days");

            // Интервал не должен улететь в космос. 
            // Рост присутствует, но подавлен. Порог скорректирован до реалистичных математических пределов FSRS.
            Assert.True(card.ScheduledDays < baseInterval * 4.0);
        }

        [Fact]
        public void TC_11_6_DelayedTriumph_LateHard()
        {
            var fsrs = new FSRSService();
            var card = CreateNewCard();
            var now = DateTime.UtcNow;

            fsrs.ApplyRating(card, Rating.Good, now);
            now = now.AddDays(Math.Max(1, card.ScheduledDays));
            fsrs.ApplyRating(card, Rating.Good, now); // Интервал ~3-5 дней

            var expectedInterval = card.ScheduledDays;
            _output.WriteLine($"[TC_11_6] Target Interval: {expectedInterval} days");

            // Открывает через 40 дней (сильное опоздание) и жмет Hard
            var lateNow = now.AddDays(40);
            fsrs.ApplyRating(card, Rating.Hard, lateNow);

            _output.WriteLine($"[TC_11_6] Result Interval: {card.ScheduledDays} days");
            _output.WriteLine($"[TC_11_6] Result Difficulty: {card.Difficulty:F4}");

            // Награда за удержание в памяти при R ~ 0 должна дать интервал больше, чем был запланирован
            Assert.True(card.ScheduledDays > expectedInterval);
            Assert.True(card.Difficulty > 5.0, "Difficulty should increase due to Hard rating");
        }

        [Fact]
        public void TC_11_7_MicroLapses_SameDay()
        {
            var fsrs = new FSRSService();
            var card = CreateNewCard();
            var now = DateTime.UtcNow;

            fsrs.ApplyRating(card, Rating.Again, now);
            fsrs.ApplyRating(card, Rating.Again, now.AddMinutes(10));
            fsrs.ApplyRating(card, Rating.Again, now.AddMinutes(15));
            fsrs.ApplyRating(card, Rating.Good, now.AddMinutes(20));

            _output.WriteLine($"[TC_11_7] S: {card.Stability:F4}, D: {card.Difficulty:F4}, Interval: {card.ScheduledDays}");

            Assert.True(card.Difficulty <= 10.0);
            Assert.True(card.Stability > 0.0); // Защита от одного дня: S не должна уйти в 0
        }

        [Fact]
        public void TC_11_8_PlateauEffect_GoodHardAlternating()
        {
            var fsrs = new FSRSService();
            var card = CreateNewCard();
            var now = DateTime.UtcNow;

            for (int i = 0; i < 20; i++)
            {
                var rating = i % 2 == 0 ? Rating.Good : Rating.Hard;
                fsrs.ApplyRating(card, rating, now);
                now = now.AddDays(Math.Max(1, card.ScheduledDays));
            }

            _output.WriteLine($"[TC_11_8] Final S: {card.Stability:F4}, D: {card.Difficulty:F4}");

            // Сложность должна стабилизироваться в верхней половине (обычно 6.0 - 9.0)
            Assert.InRange(card.Difficulty.Value, 5.5, 9.5);
            Assert.True(card.ScheduledDays > 30, "Interval should still grow exponentially despite Hards");
        }

        // ====================================================================
        // 12. Разумность оценки Easy (Sanity Checks)
        // ====================================================================

        [Fact]
        public void TC_12_1_GeniusSyndrome_ConstantEasy()
        {
            var fsrs = new FSRSService();
            var card = CreateNewCard();
            var now = DateTime.UtcNow;

            for (int i = 1; i <= 5; i++)
            {
                fsrs.ApplyRating(card, Rating.Easy, now);
                now = now.AddDays(Math.Max(1, card.ScheduledDays));
                _output.WriteLine($"[TC_12_1] Easy {i} -> Interval: {card.ScheduledDays}, D: {card.Difficulty:F4}");
            }

            Assert.True(card.Difficulty.Value <= 2.0, "Difficulty should drop near minimum");
            Assert.True(card.ScheduledDays < 36500, "Interval must not exceed absolute logic max limits");
        }

        [Fact]
        public void TC_12_2_FalseConfidence_EasyToAgain()
        {
            var fsrs = new FSRSService();
            var card = CreateNewCard();
            var now = DateTime.UtcNow;

            // 3x Easy
            for (int i = 0; i < 3; i++)
            {
                fsrs.ApplyRating(card, Rating.Easy, now);
                now = now.AddDays(Math.Max(1, card.ScheduledDays));
            }

            var dBefore = card.Difficulty.Value;
            var sBefore = card.Stability.Value;
            _output.WriteLine($"[TC_12_2] Before Again -> S: {sBefore:F4}, D: {dBefore:F4}");

            // Упс, забыл
            fsrs.ApplyRating(card, Rating.Again, now);

            _output.WriteLine($"[TC_12_2] After Again  -> S: {card.Stability:F4}, D: {card.Difficulty:F4}");

            Assert.True(card.Difficulty > dBefore + 3.0, "Difficulty should spike aggressively");
            Assert.True(card.Stability < sBefore * 0.3, "Stability should crash heavily");
        }

        [Fact]
        public void TC_12_3_QuickEpiphany_AgainToEasy()
        {
            var fsrs = new FSRSService();
            var card = CreateNewCard();
            var now = DateTime.UtcNow;

            // Набиваем историю
            fsrs.ApplyRating(card, Rating.Good, now);
            now = now.AddDays(Math.Max(1, card.ScheduledDays));
            fsrs.ApplyRating(card, Rating.Good, now);

            // Провал
            now = now.AddDays(Math.Max(1, card.ScheduledDays));
            fsrs.ApplyRating(card, Rating.Again, now);
            var sAfterLapse = card.Stability.Value;

            // Прозрение через 10 минут
            now = now.AddMinutes(10);
            fsrs.ApplyRating(card, Rating.Easy, now);

            _output.WriteLine($"[TC_12_3] S after Lapse: {sAfterLapse:F4}");
            _output.WriteLine($"[TC_12_3] S after Epiphany (Easy): {card.Stability:F4}");

            Assert.True(card.Stability > sAfterLapse, "Stability should rebound significantly");
            Assert.True(card.ScheduledDays > 0, "Interval should be restored to days, not minutes");
        }

        [Fact]
        public void TC_12_4_CruisingSpeed_GoodEasyAlternating()
        {
            var fsrs = new FSRSService();
            var card = CreateNewCard();
            var now = DateTime.UtcNow;

            var previousInterval = 0;

            for (int i = 0; i < 10; i++)
            {
                var rating = i % 2 == 0 ? Rating.Good : Rating.Easy;
                fsrs.ApplyRating(card, rating, now);
                now = now.AddDays(Math.Max(1, card.ScheduledDays));

                _output.WriteLine($"[TC_12_4] Step {i} ({rating}) -> Interval: {card.ScheduledDays}");

                // Fuzzing (случайный разброс интервалов FSRS) может иногда выдать 
                // интервал чуть меньше предыдущего. Оставляем допуск в 20% на дисперсию.
                Assert.True(card.ScheduledDays >= previousInterval * 0.8,
                    $"Interval dropped too much: {previousInterval} -> {card.ScheduledDays}");

                previousInterval = card.ScheduledDays;
            }

            _output.WriteLine($"[TC_12_4] Final D: {card.Difficulty:F4}, Final Interval: {card.ScheduledDays}");

            // Сложность должна быть низкой, но балансировать из-за Good
            Assert.InRange(card.Difficulty.Value, 1.0, 7.0);
        }
    }
}