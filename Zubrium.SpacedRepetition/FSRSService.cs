using System;
using System.Collections.Generic;
using FSRS.Core.Configurations;
using FSRS.Core.Enums;
using FSRS.Core.Interfaces;
using FSRS.Core.Services;
using Zubrium.Domain;

namespace Zubrium.SpacedRepetition
{
    public class FSRSService : ISpacedRepetitionService
    {
        private readonly IScheduler _scheduler;

        public FSRSService()
        {
            // Используем стандартную фабрику и параметры FSRS
            var options = new SchedulerOptions();
            var factory = new SchedulerFactory(options);
            _scheduler = factory.CreateScheduler();
        }

        public Dictionary<Rating, int> GetReviewOptions(Card domainCard, DateTime now)
        {
            if (now.Kind != DateTimeKind.Utc) now = now.ToUniversalTime();

            var fsrsCard = MapToFsrsCard(domainCard);
            var options = new Dictionary<Rating, int>();
            var ratings = new[] { Rating.Again, Rating.Hard, Rating.Good, Rating.Easy };

            foreach (var r in ratings)
            {
                // Клонируем объект, чтобы симуляция не изменила оригинал
                var tempCard = fsrsCard.Clone(); 
                var (updatedCard, _) = _scheduler.ReviewCard(tempCard, r, now);
                options[r] = (updatedCard.Due - now).Days;
            }

            return options;
        }

        public void ApplyRating(Card domainCard, Rating rating, DateTime now)
        {
            if (now.Kind != DateTimeKind.Utc) now = now.ToUniversalTime();

            var fsrsCard = MapToFsrsCard(domainCard);

            // FSRS.Core возвращает обновленную карточку и лог
            var (updatedCard, _) = _scheduler.ReviewCard(fsrsCard, rating, now);

            MapToDomainCard(updatedCard, domainCard, rating, now);
        }

        public void ResetProgress(Card domainCard)
        {
            domainCard.State = 1; // State.Learning
            domainCard.Step = 0;
            domainCard.Due = DateTime.UtcNow;
            domainCard.Stability = null;
            domainCard.Difficulty = null;
            domainCard.ElapsedDays = 0;
            domainCard.ScheduledDays = 0;
            domainCard.Reps = 0;
            domainCard.Lapses = 0;
            domainCard.LastReview = null;
        }

        private FSRS.Core.Models.Card MapToFsrsCard(Card domainCard)
        {
            // Убеждаемся, что State валиден для FSRS
            var state = domainCard.State == 0 ? State.Learning : (State)domainCard.State;

            return new FSRS.Core.Models.Card(
                cardId: Guid.NewGuid(), // Генерируем налету, так как он нужен только для ReviewLog, который мы пока не пишем в БД
                state: state,
                step: domainCard.Step,
                stability: domainCard.Stability,
                difficulty: domainCard.Difficulty,
                due: domainCard.Due,
                lastReview: domainCard.LastReview
            );
        }

        private void MapToDomainCard(FSRS.Core.Models.Card fsrsCard, Card domainCard, Rating rating, DateTime now)
        {
            domainCard.State = (int)fsrsCard.State;
            domainCard.Step = fsrsCard.Step;
            domainCard.Stability = fsrsCard.Stability;
            domainCard.Difficulty = fsrsCard.Difficulty;
            domainCard.Due = fsrsCard.Due;
            domainCard.LastReview = fsrsCard.LastReview;

            // Ведем аналитику сессий на стороне приложения
            domainCard.Reps++;
            if (rating == Rating.Again)
            {
                domainCard.Lapses++;
            }

            if (fsrsCard.LastReview.HasValue)
            {
                domainCard.ElapsedDays = (now.Date - fsrsCard.LastReview.Value.Date).Days;
            }
            domainCard.ScheduledDays = (fsrsCard.Due.Date - now.Date).Days;
        }
    }
}
