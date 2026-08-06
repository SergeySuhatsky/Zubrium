using Zubrium.Domain;
using Zubrium.Persistence.Entities;

namespace Zubrium.Persistence.Mappers
{
    public static class CardMapper
    {
        public static CardEntity ToEntity(this Card card)
        {
            if (card == null) return null;

            return new CardEntity
            {
                Id = card.Id,
                Title = card.Title,
                FrontMarkdown = card.FrontMarkdown,
                BriefMarkdown = card.BriefMarkdown,
                DetailedMarkdown = card.DetailedMarkdown,
                CategoryId = card.CategoryId,
                IsKnown = card.IsKnown,
                State = card.State,
                Step = card.Step,
                Due = card.Due,
                Stability = card.Stability,
                Difficulty = card.Difficulty,
                ElapsedDays = card.ElapsedDays,
                ScheduledDays = card.ScheduledDays,
                Reps = card.Reps,
                Lapses = card.Lapses,
                LastReview = card.LastReview
            };
        }

        public static Card ToDomain(this CardEntity cardEntity) 
        {
            if (cardEntity == null) return null;

            return new Card
            (
                id: cardEntity.Id,
                title: cardEntity.Title,
                frontMarkdown: cardEntity.FrontMarkdown,
                briefMarkdown: cardEntity.BriefMarkdown,
                detailedMarkdown: cardEntity.DetailedMarkdown,
                categoryId: cardEntity.CategoryId
            )
            {
                IsKnown = cardEntity.IsKnown,
                State = cardEntity.State,
                Step = cardEntity.Step,
                Due = cardEntity.Due,
                Stability = cardEntity.Stability,
                Difficulty = cardEntity.Difficulty,
                ElapsedDays = cardEntity.ElapsedDays,
                ScheduledDays = cardEntity.ScheduledDays,
                Reps = cardEntity.Reps,
                Lapses = cardEntity.Lapses,
                LastReview = cardEntity.LastReview
            };
        }
    }
}
