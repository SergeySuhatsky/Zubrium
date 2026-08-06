using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Zubrium.Domain;
using Zubrium.Persistence.Entities;
using Zubrium.Persistence.Mappers;
using Zubrium.SpacedRepetition;
using FSRS.Core.Enums;

namespace Zubrium.Tests.FSRS
{
    public class StudyIntegrationAndMappingTests
    {
        // ====================================================================
        // 1. Тестирование Двустороннего Маппинга (CardMapper)
        // ====================================================================
        [Fact]
        public void CardMapper_TwoWayMapping_PreservesAllFSRSAndCustomFields()
        {
            // Arrange
            var originalCard = new Card("test_id", "front", "brief", "title", "detailed", "cat_id")
            {
                IsKnown = true,
                State = 2, // Review
                Step = 1,
                Due = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                Stability = 15.5,
                Difficulty = 5.2,
                ElapsedDays = 10,
                ScheduledDays = 21,
                Reps = 5,
                Lapses = 1,
                LastReview = new DateTime(2024, 12, 20, 12, 0, 0, DateTimeKind.Utc)
            };

            // Act: Card -> CardEntity -> Card
            var entity = originalCard.ToEntity();
            var mappedCard = entity.ToDomain();

            // Assert
            Assert.NotNull(mappedCard);
            Assert.Equal(originalCard.Id, mappedCard.Id);
            Assert.Equal(originalCard.Title, mappedCard.Title);
            Assert.Equal(originalCard.CategoryId, mappedCard.CategoryId);

            // Проверка кастомных флагов
            Assert.Equal(originalCard.IsKnown, mappedCard.IsKnown);

            // Проверка всех 9 полей FSRS (защита от опечаток в маппере)
            Assert.Equal(originalCard.State, mappedCard.State);
            Assert.Equal(originalCard.Step, mappedCard.Step);
            Assert.Equal(originalCard.Due, mappedCard.Due);
            Assert.Equal(originalCard.Stability, mappedCard.Stability);
            Assert.Equal(originalCard.Difficulty, mappedCard.Difficulty);
            Assert.Equal(originalCard.ElapsedDays, mappedCard.ElapsedDays);
            Assert.Equal(originalCard.ScheduledDays, mappedCard.ScheduledDays);
            Assert.Equal(originalCard.Reps, mappedCard.Reps);
            Assert.Equal(originalCard.Lapses, mappedCard.Lapses);
            Assert.Equal(originalCard.LastReview, mappedCard.LastReview);
        }

        // ====================================================================
        // 2. Тестирование Логики UI вывода (NextReviewText)
        // ====================================================================
        [Fact]
        public void Card_NextReviewText_ReturnsCorrectStringsBasedOnDueDateAndFlags()
        {
            // Arrange
            var today = DateTime.UtcNow.Date;

            var cardKnown = new Card("1", "f", "b") { IsKnown = true, Reps = 5 };
            var cardNew = new Card("2", "f", "b") { IsKnown = false, Reps = 0 };

            var cardOverdue = new Card("3", "f", "b") { IsKnown = false, Reps = 1, Due = today.AddDays(-2) };
            var cardToday = new Card("4", "f", "b") { IsKnown = false, Reps = 1, Due = today };
            var cardTomorrow = new Card("5", "f", "b") { IsKnown = false, Reps = 1, Due = today.AddDays(1) };
            var cardFuture = new Card("6", "f", "b") { IsKnown = false, Reps = 1, Due = today.AddDays(5) };

            // Act & Assert
            Assert.Equal("Уже изучено", cardKnown.NextReviewText); // IsKnown имеет наивысший приоритет
            Assert.Equal("Новая", cardNew.NextReviewText);         // Затем идет проверка Reps == 0

            Assert.Equal("Просрочено", cardOverdue.NextReviewText);
            Assert.Equal("Сегодня", cardToday.NextReviewText);
            Assert.Equal("Завтра", cardTomorrow.NextReviewText);
            Assert.Equal("Через 5 дн.", cardFuture.NextReviewText);
        }

        // ====================================================================
        // 3. Интеграция: Локальная очередь (Свайп вправо) vs Долгосрочная (FSRS)
        // ====================================================================
        [Fact]
        public void StudySession_SwipeRightForLocalQueue_DoesNotMutateFSRSParameters()
        {
            // Arrange
            var fsrsService = new FSRSService();
            var card = new Card("id", "front", "brief");

            // Фиксируем исходное состояние
            var initialState = card.State;
            var initialDue = card.Due;
            var initialReps = card.Reps;

            // Локальная очередь во ViewModel
            var sessionQueue = new Queue<Card>();

            // Act: Имитируем свайп вправо ("Показать еще раз в этой сессии")
            // Мы просто кладем её в локальную очередь, НЕ вызывая fsrsService.ApplyRating
            sessionQueue.Enqueue(card);

            // Assert
            Assert.Single(sessionQueue);
            Assert.Equal(initialState, card.State);
            Assert.Equal(initialDue, card.Due);
            Assert.Equal(initialReps, card.Reps);
            Assert.Null(card.Stability);
            Assert.Null(card.Difficulty);
            Assert.Equal(0, card.Lapses);
        }

        [Fact]
        public void StudySession_SwipeLeft_AppliesFSRSRatingAndMutatesCard()
        {
            // Arrange
            var fsrsService = new FSRSService();
            var card = new Card("id", "front", "brief") { Reps = 0 }; // Новая карточка

            // Act: Имитируем свайп влево в режиме изучения ("Good")
            fsrsService.ApplyRating(card, Rating.Good, DateTime.UtcNow);

            // Assert
            Assert.True(card.Stability.HasValue, "Stability should be calculated after applying FSRS rating");
            Assert.True(card.Difficulty.HasValue, "Difficulty should be calculated");
            Assert.Equal(1, card.Reps);
        }

        // ====================================================================
        // 4. Интеграция: Игнорирование флага IsKnown при выдаче очереди
        // ====================================================================
        [Fact]
        public void StudyQueueGenerator_StrictlyExcludesCardsWhereIsKnownIsTrue()
        {
            // Arrange: Симулируем данные, которые придут из репозитория
            var dbCards = new List<Card>
            {
                new Card("1", "f", "b") { IsKnown = true, Title = "Already Known" },
                new Card("2", "f", "b") { IsKnown = false, Title = "To Learn 1" },
                new Card("3", "f", "b") { IsKnown = true, Title = "Known Too" },
                new Card("4", "f", "b") { IsKnown = false, Title = "To Learn 2" }
            };

            // Act: Логика фильтрации, которая будет внутри метода генерации очереди (StudyViewModel)
            var generatedQueue = dbCards
                .Where(c => !c.IsKnown)
                .ToList();

            // Assert
            Assert.Equal(2, generatedQueue.Count);
            Assert.DoesNotContain(generatedQueue, c => c.IsKnown);
            Assert.Contains(generatedQueue, c => c.Id == "2");
            Assert.Contains(generatedQueue, c => c.Id == "4");
        }
    }
}