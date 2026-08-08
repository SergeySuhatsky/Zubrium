using FSRS.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using Zubrium.Domain;
using Zubrium.SpacedRepetition;

namespace Zubrium.Tests.FSRS
{
    public class FSRSDifficultyAndStabilityTests
    {
        // Фабрика для создания новой карточки
        private Card CreateNewCard()
        {
            return new Card(
                id: Guid.NewGuid().ToString("N"),
                frontMarkdown: "Front",
                briefMarkdown: "Brief"
            );
        }

        // ==========================================
        // 2. Изменение сложности (Difficulty Updates)
        // ==========================================

        [Fact]
        public void TC_2_1_Difficulty_Increases_On_Hard_Or_Again()
        {
            // Arrange
            var fsrs = new FSRSService();
            var card = CreateNewCard();
            var now = DateTime.UtcNow;

            // Инициализируем карточку средней оценкой
            fsrs.ApplyRating(card, Rating.Good, now);
            var initialDifficulty = card.Difficulty.Value;

            // Act: Даем плохую оценку при следующем повторении (через 1 день)
            fsrs.ApplyRating(card, Rating.Again, now.AddDays(1));

            // Assert
            Assert.True(card.Difficulty.Value > initialDifficulty, "Difficulty should increase after 'Again'");
            Assert.True(card.Difficulty.Value <= 10.0, "Difficulty should not exceed the upper bound of 10.0");
        }

        [Fact]
        public void TC_2_2_Difficulty_Decreases_On_Easy()
        {
            // Arrange
            var fsrs = new FSRSService();
            var card = CreateNewCard();
            var now = DateTime.UtcNow;

            // Делаем карточку сложной
            fsrs.ApplyRating(card, Rating.Hard, now);
            var initialDifficulty = card.Difficulty.Value;

            // Act: Даем легкую оценку
            fsrs.ApplyRating(card, Rating.Easy, now.AddDays(1));

            // Assert
            Assert.True(card.Difficulty.Value < initialDifficulty, "Difficulty should decrease after 'Easy'");
            Assert.True(card.Difficulty.Value >= 1.0, "Difficulty should not drop below the lower bound of 1.0");
        }

        [Fact]
        public void TC_2_3_Difficulty_Mean_Reversion()
        {
            // Arrange
            var fsrs = new FSRSService();
            var card = CreateNewCard();
            var now = DateTime.UtcNow;

            // Намеренно загоняем сложность на высокий уровень
            fsrs.ApplyRating(card, Rating.Again, now);
            fsrs.ApplyRating(card, Rating.Again, now.AddDays(1));
            var highDifficulty = card.Difficulty.Value;

            // Act: Даем серию нейтральных оценок (Good)
            fsrs.ApplyRating(card, Rating.Good, now.AddDays(3));
            var rev1 = card.Difficulty.Value;

            fsrs.ApplyRating(card, Rating.Good, now.AddDays(10));
            var rev2 = card.Difficulty.Value;

            // Assert
            // При нейтральных оценках экстремальная сложность должна возвращаться к среднему
            Assert.True(rev1 < highDifficulty, "Difficulty should start reverting to mean");
            Assert.True(rev2 < rev1, "Difficulty should continue reverting asymptotically");
        }

        // ==========================================
        // 3. Успешные повторения (Stability on Success)
        // ==========================================

        [Fact]
        public void TC_3_1_Standard_Stability_Growth()
        {
            // Arrange
            var fsrs = new FSRSService();
            var card = CreateNewCard();
            var now = DateTime.UtcNow;

            // Изучаем новую карточку
            fsrs.ApplyRating(card, Rating.Good, now);
            var initialStability = card.Stability.Value;
            var dueTime = card.Due; // Дата запланированного повторения

            // Act: Повторяем карточку ровно в срок
            fsrs.ApplyRating(card, Rating.Good, dueTime);

            // Assert
            Assert.True(card.Stability.Value > initialStability, "Stability should grow after a successful review");
        }

        [Fact]
        public void TC_3_2_Stability_Growth_Depends_On_Rating()
        {
            // Arrange
            var fsrs = new FSRSService();
            var now = DateTime.UtcNow;

            var cardHard = CreateNewCard();
            var cardGood = CreateNewCard();
            var cardEasy = CreateNewCard();

            // Синхронизируем базовое состояние (все три карточки идентичны)
            fsrs.ApplyRating(cardHard, Rating.Good, now);
            fsrs.ApplyRating(cardGood, Rating.Good, now);
            fsrs.ApplyRating(cardEasy, Rating.Good, now);

            // Берем запланированную дату (она у всех одинаковая)
            var dueTime = cardGood.Due;

            // Act
            fsrs.ApplyRating(cardHard, Rating.Hard, dueTime);
            fsrs.ApplyRating(cardGood, Rating.Good, dueTime);
            fsrs.ApplyRating(cardEasy, Rating.Easy, dueTime);

            // Assert
            Assert.True(cardEasy.Stability.Value > cardGood.Stability.Value, "Easy review should yield higher stability than Good");
            Assert.True(cardGood.Stability.Value > cardHard.Stability.Value, "Good review should yield higher stability than Hard");
        }

        [Fact]
        public void TC_3_3_Late_Review_Harder_To_Recall_Greater_Reward()
        {
            // Arrange
            var fsrs = new FSRSService();
            var now = DateTime.UtcNow;

            var cardOnTime = CreateNewCard();
            var cardLate = CreateNewCard();

            fsrs.ApplyRating(cardOnTime, Rating.Good, now);
            fsrs.ApplyRating(cardLate, Rating.Good, now);

            var dueTime = cardOnTime.Due;
            var lateTime = dueTime.AddDays(30); // Значительное опоздание, R (Retrievability) низкая

            // Act
            fsrs.ApplyRating(cardOnTime, Rating.Good, dueTime);
            fsrs.ApplyRating(cardLate, Rating.Good, lateTime);

            // Assert
            // FSRS вознаграждает за успешное вспоминание старого материала бóльшим ростом S
            Assert.True(cardLate.Stability.Value > cardOnTime.Stability.Value, "Late successful review should reward with higher stability growth");
        }

        [Fact]
        public void TC_3_4_Early_Review_Yields_Minimal_Growth()
        {
            // Arrange
            var fsrs = new FSRSService();
            var now = DateTime.UtcNow;

            var cardOnTime = CreateNewCard();
            var cardEarly = CreateNewCard();

            fsrs.ApplyRating(cardOnTime, Rating.Good, now);
            fsrs.ApplyRating(cardEarly, Rating.Good, now);

            var dueTime = cardOnTime.Due;
            var earlyTime = now.AddHours(12); // Слишком раннее повторение (на следующий день или в тот же), R близка к 100%

            // Act
            fsrs.ApplyRating(cardOnTime, Rating.Good, dueTime);
            fsrs.ApplyRating(cardEarly, Rating.Good, earlyTime);

            // Assert
            // Ранний просмотр почти не должен прибавлять стабильность, так как карточка и так свежа в памяти
            Assert.True(cardEarly.Stability.Value <= cardOnTime.Stability.Value, "Early review should yield significantly smaller stability growth compared to an on-time review");
        }
    }
}
