using System;
using System.Collections.Generic;
using System.Text;
using Zubrium.Domain;
using Zubrium.Persistence;

namespace Zubrium.Content.Repository
{
    public interface IContentRepository
    {
        public async Task<ArticleEntity> GetArticleAsync(string articleId)
        {
            throw new NotImplementedException();
        }

        public async Task PushData()
        {
            throw new NotImplementedException();
        }
    }
}
