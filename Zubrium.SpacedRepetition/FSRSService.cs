using System;
using FSRS.Core;
using FSRS.Core.Models;
using Zubrium.Domain;

namespace Zubrium.SpacedRepetition
{
    public class FSRSService : ISpacedRepetitionService
    {
        private readonly FSRS.Core.FSRS _fsrs;

        public FSRSService()
        {
            _fsrs = new FSRS.Core.FSRS();
        }

        public void ApplyRating(Card domainCard, Rating rating, DateTime now)
        {
            var fsrsCard = MapToFsrsCard(domainCard);
            var options = _fsrs.Repeat(fsrsCard, now);

            if (options.TryGetValue(rating, out var schedulingInfo))
            {
                MapToDomainCard(schedulingInfo.Card, domainCard);
            }
        }

        public void ResetProgress(Card domainCard)
        {
            domainCard.State = 0;
            domainCard.Due = DateTime.UtcNow;
            domainCard.Stability = 0;
            domainCard.Difficulty = 0;
            domainCard.ElapsedDays = 0;
            domainCard.ScheduledDays = 0;
            domainCard.Reps = 0;
            domainCard.Lapses = 0;
            domainCard.LastReview = null;
        }

        private FSRS.Core.Models.Card MapToFsrsCard(Card domainCard)
        {
            return new FSRS.Core.Models.Card
            {
                State = (State)domainCard.State,
                Due = domainCard.Due,
                Stability = domainCard.Stability,
                Difficulty = domainCard.Difficulty,
                ElapsedDays = domainCard.ElapsedDays,
                ScheduledDays = domainCard.ScheduledDays,
                Reps = domainCard.Reps,
                Lapses = domainCard.Lapses,
                LastReview = domainCard.LastReview
            };
        }

        private void MapToDomainCard(FSRS.Core.Models.Card fsrsCard, Card domainCard)
        {
            domainCard.State = (int)fsrsCard.State;
            domainCard.Due = fsrsCard.Due;
            domainCard.Stability = fsrsCard.Stability;
            domainCard.Difficulty = fsrsCard.Difficulty;
            domainCard.ElapsedDays = fsrsCard.ElapsedDays;
            domainCard.ScheduledDays = fsrsCard.ScheduledDays;
            domainCard.Reps = fsrsCard.Reps;
            domainCard.Lapses = fsrsCard.Lapses;
            domainCard.LastReview = fsrsCard.LastReview;
        }
    }
}
