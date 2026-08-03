using SQLite;
using System;
using System.Collections.Generic;
using System.Text;
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
                CategoryId = card.CategoryId
            };
        }

        public static Card ToDomain (this CardEntity cardEntity) 
        {
            if (cardEntity == null) return null;

            return new Card
            (
                id : cardEntity.Id,
                title : cardEntity.Title,
                frontMarkdown : cardEntity.FrontMarkdown,
                briefMarkdown : cardEntity.BriefMarkdown,
                detailedMarkdown : cardEntity.DetailedMarkdown,
                categoryId : cardEntity.CategoryId
            );
        }
    }
}
