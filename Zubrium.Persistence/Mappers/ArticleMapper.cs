using System;
using System.Collections.Generic;
using System.Text;
using Zubrium.Domain;
using Zubrium.Persistence.Entities;

namespace Zubrium.Persistence.Mappers
{
    public static class ArticleMapper
    {
        public static ArticleEntity ToEntity(this Article article)
        {
            if (article == null) return null;

            return new ArticleEntity
            {
                Id = article.Id,
                Title = article.Title,
                BodyMarkdown = article.BodyMarkdown,
                CategoryId = article.CategoryId
            };
        }

        public static Article ToDomain (this ArticleEntity articleEntity) 
        {
            if (articleEntity == null) return null;

            return new Article
            (
                id: articleEntity.Id,
                title: articleEntity.Title,
                bodyMarkdown: articleEntity.BodyMarkdown,
                categoryId: articleEntity.CategoryId
            );
        }
    }
}
