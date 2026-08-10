using System.Collections.Generic;
using System.Text.Json;
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
                IsMastered = card.IsMastered,
                Due = card.Due,
                Reps = card.Reps,
                LastReview = card.LastReview,

                // Сериализуем словарь AlgorithmData в JSON-строку для базы
                AlgorithmDataJson = JsonSerializer.Serialize(card.AlgorithmData)
            };
        }

        public static Card ToDomain(this CardEntity cardEntity) 
        {
            if (cardEntity == null) return null;

            // Десериализуем JSON обратно в Dictionary, а если поле пустое — создаем пустой словарь
            Dictionary<string, string> algorithmData = null;
            if (!string.IsNullOrEmpty(cardEntity.AlgorithmDataJson))
            {
                try
                {
                    algorithmData = JsonSerializer.Deserialize<Dictionary<string, string>>(cardEntity.AlgorithmDataJson);
                }
                catch
                {
                    // Резервный фоллбэк на случай поврежденной строки
                    algorithmData = new Dictionary<string, string>();
                }
            }

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
                IsMastered = cardEntity.IsMastered,
                Due = cardEntity.Due,
                Reps = cardEntity.Reps,
                LastReview = cardEntity.LastReview,

                // Присваиваем десериализованный словарь обратно карточке
                AlgorithmData = algorithmData ?? new Dictionary<string, string>()
            };
        }
    }
}
