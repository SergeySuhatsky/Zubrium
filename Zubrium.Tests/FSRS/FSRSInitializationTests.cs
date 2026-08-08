using FSRS.Core.Enums;
using System;
using Xunit;
using Zubrium.Domain;
using Zubrium.SpacedRepetition;
namespace Zubrium.Tests.FSRS
{
    public class FSRSInitializationTests
    {
        // Фабрика для создания чистой карточки (по умолчанию State = 1, Step = 0)
        private Card CreateNewCard()
        {
            return new Card(
                id: Guid.NewGuid().ToString("N"),
                frontMarkdown: "Front",
                briefMarkdown: "Brief"
            );
        }

        [Fact]
        public void TC_1_1_InitialStability_DependsOnRating()
        {
            // Arrange
            var fsrs = new FSRSService();
            var now = DateTime.UtcNow;

            var cardAgain = CreateNewCard();
            var cardHard = CreateNewCard();
            var cardGood = CreateNewCard();
            var cardEasy = CreateNewCard();

            // Act - ApplyRating мутирует саму карточку
            fsrs.ApplyRating(cardAgain, Rating.Again, now);
            fsrs.ApplyRating(cardHard, Rating.Hard, now);
            fsrs.ApplyRating(cardGood, Rating.Good, now);
            fsrs.ApplyRating(cardEasy, Rating.Easy, now);

            // Assert
            // Убеждаемся, что алгоритм рассчитал значения
            Assert.True(cardAgain.Stability.HasValue, "Stability should not be null after review");
            Assert.True(cardHard.Stability.HasValue, "Stability should not be null after review");
            Assert.True(cardGood.Stability.HasValue, "Stability should not be null after review");
            Assert.True(cardEasy.Stability.HasValue, "Stability should not be null after review");

            // Проверяем, что стабильность строго возрастает от худшей оценки к лучшей
            Assert.True(cardAgain.Stability.Value < cardHard.Stability.Value, "Stability (Again) should be less than Stability (Hard)");
            Assert.True(cardHard.Stability.Value < cardGood.Stability.Value, "Stability (Hard) should be less than Stability (Good)");
            Assert.True(cardGood.Stability.Value < cardEasy.Stability.Value, "Stability (Good) should be less than Stability (Easy)");
        }

        [Fact]
        public void TC_1_2_InitialDifficulty_IsWithinBounds()
        {
            // Arrange
            var fsrs = new FSRSService();
            var card = CreateNewCard();

            // Act
            fsrs.ApplyRating(card, Rating.Good, DateTime.UtcNow);

            // Assert
            Assert.True(card.Difficulty.HasValue, "Difficulty should not be null after review");

            // Сложность всегда должна быть в пределах от 1.0 до 10.0
            Assert.InRange(card.Difficulty.Value, 1.0, 10.0);
        }

        [Fact]
        public void TC_1_3_ResetProgress_ClearsFSRSDatAndAnalytics()
        {
            // Учитывая ваш код, полезно протестировать метод ResetProgress
            // Arrange
            var fsrs = new FSRSService();
            var card = CreateNewCard();

            // Симулируем, что карточку уже учили
            fsrs.ApplyRating(card, Rating.Hard, DateTime.UtcNow);

            // Act
            fsrs.ResetProgress(card);

            // Assert
            Assert.Equal(1, card.State); // 1 = Learning
            Assert.Equal(0, card.Step);
            Assert.Null(card.Stability);
            Assert.Null(card.Difficulty);
            Assert.Equal(0, card.ElapsedDays);
            Assert.Equal(0, card.ScheduledDays);
            Assert.Equal(0, card.Reps);
            Assert.Equal(0, card.Lapses);
            Assert.Null(card.LastReview);
        }
    }
}
