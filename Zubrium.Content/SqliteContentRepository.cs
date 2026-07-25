using Zubrium.Domain;
using Zubrium.Persistence;
using SQLite;

namespace Zubrium.Content
{
    public class SqliteContentRepository : IContentRepository
    {
        private readonly SQLiteAsyncConnection _db;

        public SqliteContentRepository(string dbPath)
        {
            _db = new SQLiteAsyncConnection(dbPath);
            _db.CreateTableAsync<ArticleEntity>().Wait();
        }

        public async Task<ArticleEntity> GetArticleAsync(string articleId)
        {
            return await _db.Table<ArticleEntity>().Where(a => a.Id == articleId).FirstOrDefaultAsync();
        }

        public async Task PushData() 
        {
            await _db.InsertAsync
                (
                    new List<ArticleEntity>
                    {
                        new ArticleEntity
                        {
                            Id = "1",
                            Title = "Sample Article 1",
                            BodyMarkdown = "This is the content of sample article 1."
                        },
                        new ArticleEntity
                        {
                            Id = "2",
                            Title = "Sample Article 2",
                            BodyMarkdown = "This is the content of sample article 2."
                        }
                    }
                );
        }
    }
}
