using FSRS.Core.Configurations;
using FSRS.Core.Enums;
using FSRS.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit.Abstractions;
using Zubrium.Domain;
using Zubrium.SpacedRepetition;

namespace Zubrium.Tests.FSRS
{
    public class FSRSComplexScenariosTests
    {
        private readonly ITestOutputHelper _output;

        // Внедряем ITestOutputHelper через конструктор
        public FSRSComplexScenariosTests(ITestOutputHelper output)
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

        // ==========================================
        // 6. Длинные цепочки и смена состояний
        // ==========================================

        //[Fact]
        //public void TC_6_1_FullStateMachineCycle()
        //{
        //    var fsrs = new FSRSService();
        //    var card = CreateNewCard();
        //    var now = DateTime.UtcNow;

        //    fsrs.ApplyRating(card, Rating.Good, now);
        //    _output.WriteLine($"[Шаг 1 - Good] State: {card.State}, Reps: {card.Reps}");

        //    now = now.AddDays(card.ScheduledDays);
        //    fsrs.ApplyRating(card, Rating.Again, now);
        //    _output.WriteLine($"[Шаг 2 - Again] State: {card.State}, Lapses: {card.Lapses}");

        //    now = now.AddMinutes(10);
        //    fsrs.ApplyRating(card, Rating.Good, now);
        //    _output.WriteLine($"[Шаг 3 - Good] State: {card.State}, Final Stability: {card.Stability:F4}");

        //    Assert.Equal(2, card.State);
        //    Assert.Equal(3, card.Reps);
        //}

        [Fact]
        public void TC_6_2_OscillatingRatings()
        {
            var fsrs = new FSRSService();
            var card = CreateNewCard();
            var now = DateTime.UtcNow;

            Rating[] ratings = { Rating.Good, Rating.Again, Rating.Good, Rating.Hard, Rating.Good, Rating.Easy };

            foreach (var r in ratings)
            {
                fsrs.ApplyRating(card, r, now);
                now = now.AddDays(Math.Max(1, card.ScheduledDays));
            }

            _output.WriteLine($"[TC_6_2] Final Difficulty: {card.Difficulty:F4}");
            _output.WriteLine($"[TC_6_2] Final Stability: {card.Stability:F4}");
            _output.WriteLine($"[TC_6_2] Total Lapses: {card.Lapses}");

            Assert.True(card.Difficulty.HasValue);
            Assert.InRange(card.Difficulty.Value, 1.0, 10.0);
            Assert.Equal(1, card.Lapses);
        }

        [Fact]
        public void TC_6_3_EasyStreakWithEarlyReviews()
        {
            var fsrs = new FSRSService();
            var card = CreateNewCard();
            var now = DateTime.UtcNow;

            fsrs.ApplyRating(card, Rating.Easy, now);
            var initialS = card.Stability.Value;
            _output.WriteLine($"[TC_6_3] Initial Stability (S0): {initialS:F4}");

            for (int i = 0; i < 4; i++)
            {
                now = now.AddDays(Math.Max(1, card.ScheduledDays * 0.2));
                fsrs.ApplyRating(card, Rating.Easy, now);
            }

            _output.WriteLine($"[TC_6_3] Final Difficulty: {card.Difficulty:F4}");
            _output.WriteLine($"[TC_6_3] Final Stability after 4 early reviews: {card.Stability:F4}");

            Assert.Equal(1.0, Math.Round(card.Difficulty.Value, 1));
            Assert.True(card.Stability.Value > initialS);
        }

        // ==========================================
        // 7. Комбинаторика параметров
        // ==========================================

        [Fact]
        public void TC_7_1_HighDifficulty_ExtremeLate()
        {
            var fsrs = new FSRSService();
            var card = CreateNewCard();
            var now = DateTime.UtcNow;

            fsrs.ApplyRating(card, Rating.Again, now);
            fsrs.ApplyRating(card, Rating.Again, now.AddDays(1));

            var scheduledDays = card.ScheduledDays;
            _output.WriteLine($"[TC_7_1] Difficulty before late review: {card.Difficulty:F4}");

            var extremeLate = now.AddDays(scheduledDays * 3 + 10);
            fsrs.ApplyRating(card, Rating.Good, extremeLate);

            _output.WriteLine($"[TC_7_1] Stability after extreme late review: {card.Stability:F4}");
            _output.WriteLine($"[TC_7_1] Difficulty after extreme late review: {card.Difficulty:F4}");

            Assert.True(card.Stability.HasValue);
            Assert.True(card.Stability.Value > 0);
            Assert.True(card.Difficulty.Value > 5.0);
        }

        [Fact]
        public void TC_7_3_LapseOnLongForgottenCard()
        {
            var fsrs = new FSRSService();
            var card = CreateNewCard();
            var now = DateTime.UtcNow;

            fsrs.ApplyRating(card, Rating.Good, now);
            fsrs.ApplyRating(card, Rating.Good, now.AddDays(card.ScheduledDays));

            _output.WriteLine($"[TC_7_3] Stability before 2-year pause: {card.Stability:F4}");

            var forgottenTime = now.AddDays(730);
            fsrs.ApplyRating(card, Rating.Again, forgottenTime);

            _output.WriteLine($"[TC_7_3] Stability after 2-year pause and 'Again': {card.Stability:F4}");
            _output.WriteLine($"[TC_7_3] New State: {card.State}");

            Assert.Equal(3, card.State);
            Assert.True(card.Stability.HasValue);
            Assert.True(card.Stability.Value > 0.0, "Stability shouldn't drop below 0 even after 2 years");
        }

        [Fact]
        public void TC_7_4_HardOnSuperStableCard()
        {
            var fsrs = new FSRSService();
            var card = CreateNewCard();
            var now = DateTime.UtcNow;

            fsrs.ApplyRating(card, Rating.Easy, now);
            for (int i = 0; i < 5; i++)
            {
                now = now.AddDays(card.ScheduledDays);
                fsrs.ApplyRating(card, Rating.Easy, now);
            }

            var previousInterval = card.ScheduledDays;
            _output.WriteLine($"[TC_7_4] ScheduledDays before 'Hard': {previousInterval}");

            now = now.AddDays(card.ScheduledDays);
            fsrs.ApplyRating(card, Rating.Hard, now);

            _output.WriteLine($"[TC_7_4] ScheduledDays after 'Hard': {card.ScheduledDays}");
            _output.WriteLine($"[TC_7_4] Difficulty after 'Hard': {card.Difficulty:F4}");

            Assert.Equal(2, card.State);
            Assert.True(card.ScheduledDays >= previousInterval);
        }

        // ==========================================
        // 8. Влияние профилей на длинной дистанции
        // ==========================================

        [Fact]
        public void TC_8_1_DesiredRetention_Divergence()
        {
            var strictScheduler = new SchedulerFactory(new SchedulerOptions { DesiredRetention = 0.98 }).CreateScheduler();
            var relaxedScheduler = new SchedulerFactory(new SchedulerOptions { DesiredRetention = 0.75 }).CreateScheduler();

            var cardStrict = new global::FSRS.Core.Models.Card(Guid.NewGuid());
            var cardRelaxed = new global::FSRS.Core.Models.Card(Guid.NewGuid());
            var now = DateTime.UtcNow;

            for (int i = 0; i < 5; i++)
            {
                cardStrict = strictScheduler.ReviewCard(cardStrict, Rating.Good, now).UpdatedCard;
                cardRelaxed = relaxedScheduler.ReviewCard(cardRelaxed, Rating.Good, now).UpdatedCard;
                now = now.AddDays(3);
            }

            var strictInterval = (cardStrict.Due - now).Days;
            var relaxedInterval = (cardRelaxed.Due - now).Days;

            _output.WriteLine($"[TC_8_1] Stability (Strict): {cardStrict.Stability:F4} | Interval: {strictInterval} days");
            _output.WriteLine($"[TC_8_1] Stability (Relaxed): {cardRelaxed.Stability:F4} | Interval: {relaxedInterval} days");

            Assert.Equal(cardStrict.Stability, cardRelaxed.Stability);
            Assert.Equal(cardStrict.Difficulty, cardRelaxed.Difficulty);
            Assert.True(relaxedInterval > strictInterval * 1.5);
        }

        [Fact]
        public void TC_8_3_Fuzzing_Impact()
        {
            var options = new SchedulerOptions { EnableFuzzing = true };
            var scheduler = new SchedulerFactory(options).CreateScheduler();
            var now = DateTime.UtcNow;

            var uniqueIntervals = new HashSet<int>();

            for (int i = 0; i < 50; i++)
            {
                var c = new global::FSRS.Core.Models.Card(Guid.NewGuid());
                c = scheduler.ReviewCard(c, Rating.Good, now).UpdatedCard;
                c = scheduler.ReviewCard(c, Rating.Good, now.AddDays(1)).UpdatedCard;
                c = scheduler.ReviewCard(c, Rating.Good, now.AddDays(4)).UpdatedCard;

                uniqueIntervals.Add((c.Due - now).Days);
            }

            _output.WriteLine($"[TC_8_3] Number of unique intervals generated by Fuzzing: {uniqueIntervals.Count} out of 50");
            _output.WriteLine($"[TC_8_3] Intervals: {string.Join(", ", uniqueIntervals)}");

            Assert.True(uniqueIntervals.Count > 1);
        }

        // ==========================================
        // 9. Экстремальная математика
        // ==========================================

        [Fact]
        public void TC_9_2_MultipleAgainInOneDay()
        {
            var fsrs = new FSRSService();
            var card = CreateNewCard();
            var now = DateTime.UtcNow;

            fsrs.ApplyRating(card, Rating.Again, now);
            fsrs.ApplyRating(card, Rating.Again, now.AddMinutes(5));
            fsrs.ApplyRating(card, Rating.Again, now.AddMinutes(10));
            fsrs.ApplyRating(card, Rating.Again, now.AddMinutes(15));

            _output.WriteLine($"[TC_9_2] Final Difficulty: {card.Difficulty:F4}");
            _output.WriteLine($"[TC_9_2] Final Stability: {card.Stability:F4}");

            Assert.True(card.Stability.Value > 0);
            Assert.True(card.Difficulty.Value <= 10.0);
        }

        
        

        // ==========================================
        // 12. Разумность оценки Easy
        // ==========================================

        [Fact]
        public void TC_12_2_FalseConfidence_EasyToAgain()
        {
            var fsrs = new FSRSService();
            var card = CreateNewCard();
            var now = DateTime.UtcNow;

            fsrs.ApplyRating(card, Rating.Easy, now);
            now = now.AddDays(card.ScheduledDays);
            fsrs.ApplyRating(card, Rating.Easy, now);
            now = now.AddDays(card.ScheduledDays);
            fsrs.ApplyRating(card, Rating.Easy, now);

            var stableS = card.Stability.Value;
            var lowD = card.Difficulty.Value;
            _output.WriteLine($"[TC_12_2] Before 'Again' -> S: {stableS:F4}, D: {lowD:F4}");

            now = now.AddDays(card.ScheduledDays);
            fsrs.ApplyRating(card, Rating.Again, now);

            _output.WriteLine($"[TC_12_2] After 'Again'  -> S: {card.Stability:F4}, D: {card.Difficulty:F4}");

            Assert.True(card.Stability.Value < stableS * 0.5);
            Assert.True(card.Difficulty.Value > lowD + 2.0);
        }

        [Fact]
        public void TC_12_3_QuickEpiphany_AgainToEasy()
        {
            var fsrs = new FSRSService();
            var card = CreateNewCard();
            var now = DateTime.UtcNow;

            fsrs.ApplyRating(card, Rating.Good, now);
            now = now.AddDays(card.ScheduledDays);
            fsrs.ApplyRating(card, Rating.Good, now);

            now = now.AddDays(card.ScheduledDays);
            fsrs.ApplyRating(card, Rating.Again, now);

            var postLapseStability = card.Stability.Value;
            _output.WriteLine($"[TC_12_3] Stability immediately after Lapse: {postLapseStability:F4}");

            now = now.AddMinutes(10);
            fsrs.ApplyRating(card, Rating.Easy, now);

            _output.WriteLine($"[TC_12_3] Stability after quick 'Easy' (Rebound): {card.Stability:F4}");

            Assert.True(card.Stability.Value > postLapseStability);
            Assert.Equal(2, card.State);
        }
    }
}
