using Zubrium.Domain;
using SQLite;
using Zubrium.Persistence.Entities;
using Zubrium.Persistence.Mappers;
namespace Zubrium.Content.Repository
{
    public class SqliteContentRepository : IContentRepository
    {
        private readonly SQLiteAsyncConnection _db;

        public SqliteContentRepository(string dbPath)
        {
            _db = new SQLiteAsyncConnection(dbPath);

            _db.CreateTableAsync<ArticleEntity>().Wait();
            _db.CreateTableAsync<CardEntity>().Wait();
            _db.CreateTableAsync<QuizBlockEntity>().Wait();
            _db.CreateTableAsync<CategoryEntity>().Wait();
            _db.CreateTableAsync<DailyActivityEntity>().Wait();
        }

        // ==========================================
        // МЕТОДЫ ДЛЯ СТАТЕЙ (ARTICLES)
        // ==========================================

        public async Task<ArticleEntity> GetArticleAsync(string articleId)
        {
            return await _db.Table<ArticleEntity>().Where(a => a.Id == articleId).FirstOrDefaultAsync();
        }

        public async Task<List<ArticleEntity>> GetArticlesAsync()
        {
            return await _db.Table<ArticleEntity>().ToListAsync();
        }

        public async Task<List<ArticleEntity>> GetArticlesByCategoryAsync(string categoryId)
        {
            return await _db.Table<ArticleEntity>().Where(a => a.CategoryId == categoryId).ToListAsync();
        }

        // ==========================================
        // МЕТОДЫ ДЛЯ КАРТОЧЕК (CARDS)
        // ==========================================

        public async Task<CardEntity> GetCardAsync(string cardId)
        {
            return await _db.Table<CardEntity>().Where(c => c.Id == cardId).FirstOrDefaultAsync();
        }

        public async Task<List<CardEntity>> GetCardsAsync()
        {
            return await _db.Table<CardEntity>().ToListAsync();
        }

        public async Task<List<CardEntity>> GetCardsByCategoryAsync(string categoryId)
        {
            return await _db.Table<CardEntity>().Where(c => c.CategoryId == categoryId).ToListAsync();
        }

        public async Task DeleteCardAsync(string cardId)
        {
            await _db.Table<CardEntity>().Where(c => c.Id == cardId).DeleteAsync();
        }

        // ==========================================
        // МЕТОДЫ ДЛЯ КВИЗОВ (QUIZZES)
        // ==========================================

        public async Task<QuizBlockEntity> GetQuizBlockAsync(string quizId)
        {
            return await _db.Table<QuizBlockEntity>().Where(q => q.Id == quizId).FirstOrDefaultAsync();
        }

        public async Task<List<QuizBlockEntity>> GetQuizBlocksAsync()
        {
            return await _db.Table<QuizBlockEntity>().ToListAsync();
        }

        public async Task<List<QuizBlockEntity>> GetQuizBlocksByCategoryAsync(string categoryId)
        {
            return await _db.Table<QuizBlockEntity>().Where(q => q.CategoryId == categoryId).ToListAsync();
        }

        // ==========================================
        // КАТЕГОРИИ (CATEGORIES)
        // ==========================================

        public async Task<CategoryEntity?> GetCategoryByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var normalizedName = name.Trim();
            return await _db.Table<CategoryEntity>().Where(c => c.Name == normalizedName).FirstOrDefaultAsync();
        }

        public async Task<List<CategoryEntity>> GetAllCategoriesAsync()
        {
            return await _db.Table<CategoryEntity>().ToListAsync();
        }

        public async Task<int> SaveCategoryAsync(CategoryEntity category)
        {
            return await _db.InsertOrReplaceAsync(category);
        }

        // ==========================================
        // МЕТОДЫ ИМПОРТА
        // ==========================================

        public async Task InsertContentSet(ParsedContentSet contentSet, bool isAddArticles = true, bool isAddQuizes = true, bool isAddCards = true)
        {
            // Используется InsertAllAsync для вставки коллекций целиком
            if (isAddArticles && contentSet.Articles.Count > 0)
            {
                var articleEntities = contentSet.Articles.Select(a => a.ToEntity()).ToList();
                await _db.InsertAllAsync(articleEntities);
            }

            if (isAddCards && contentSet.Cards.Count > 0)
            {
                var cardEntities = contentSet.Cards.Select(c => c.ToEntity()).ToList();
                await _db.InsertAllAsync(cardEntities);
            }

            if (isAddQuizes && contentSet.Quizzes.Count > 0)
            {
                var quizEntities = contentSet.Quizzes.Select(q => q.ToEntity()).ToList();
                await _db.InsertAllAsync(quizEntities);
            }
        }


        // ==========================================
        // СОХРАНЕНИЕ / ОБНОВЛЕНИЕ ОДИНОЧНЫХ ЭЛЕМЕНТОВ
        // ==========================================

        public async Task<int> SaveArticleAsync(ArticleEntity article)
        {
            // Вставляет запись. Если запись с таким Id уже есть — обновляет её.
            return await _db.InsertOrReplaceAsync(article);
        }

        public async Task<int> SaveCardAsync(CardEntity card)
        {
            return await _db.InsertOrReplaceAsync(card);
        }

        public async Task<int> SaveQuizBlockAsync(QuizBlockEntity quiz)
        {
            return await _db.InsertOrReplaceAsync(quiz);
        }

        // ==========================================
        // СОХРАНЕНИЕ / ОБНОВЛЕНИЕ СПИСКОВ ЭЛЕМЕНТОВ
        // ==========================================

        public async Task SaveArticlesAsync(IEnumerable<ArticleEntity> articles)
        {
            // Запуск синхронных операций обновления внутри асинхронной транзакции для скорости
            await _db.RunInTransactionAsync(conn =>
            {
                foreach (var article in articles)
                {
                    conn.InsertOrReplace(article);
                }
            });
        }

        public async Task SaveCardsAsync(IEnumerable<CardEntity> cards)
        {
            await _db.RunInTransactionAsync(conn =>
            {
                foreach (var card in cards)
                {
                    conn.InsertOrReplace(card);
                }
            });
        }

        public async Task SaveQuizBlocksAsync(IEnumerable<QuizBlockEntity> quizzes)
        {
            await _db.RunInTransactionAsync(conn =>
            {
                foreach (var quiz in quizzes)
                {
                    conn.InsertOrReplace(quiz);
                }
            });
        }

        // ==========================================
        // АНАЛИТИКА АКТИВНОСТИ
        // ==========================================

        public async Task LogDailyActivityAsync(DateTime date, int newCards, int reviewCards)
        {
            var dateOnly = date.Date;
            var existing = await _db.Table<DailyActivityEntity>().Where(a => a.Date == dateOnly).FirstOrDefaultAsync();

            if (existing != null)
            {
                existing.NewCardsStudied += newCards;
                existing.ReviewCardsStudied += reviewCards;
                await _db.UpdateAsync(existing);
            }
            else
            {
                await _db.InsertAsync(new DailyActivityEntity 
                { 
                    Date = dateOnly, 
                    NewCardsStudied = newCards, 
                    ReviewCardsStudied = reviewCards 
                });
            }
        }

        public async Task<List<DailyActivityEntity>> GetDailyActivitiesAsync()
        {
            return await _db.Table<DailyActivityEntity>().ToListAsync();
        }

        // ==========================================
        // МЕТОДЫ УДАЛЕНИЯ (ДОБАВЛЕНО)
        // ==========================================

        public async Task DeleteArticleAsync(string articleId)
        {
            await _db.Table<ArticleEntity>().Where(a => a.Id == articleId).DeleteAsync();
        }

        public async Task DeleteQuizBlockAsync(string quizId)
        {
            await _db.Table<QuizBlockEntity>().Where(q => q.Id == quizId).DeleteAsync();
        }

        public async Task DeleteCategoryAsync(string categoryId)
        {
            await _db.RunInTransactionAsync(conn =>
            {
                conn.Table<CardEntity>().Delete(c => c.CategoryId == categoryId);
                conn.Table<ArticleEntity>().Delete(a => a.CategoryId == categoryId);
                conn.Table<QuizBlockEntity>().Delete(q => q.CategoryId == categoryId);
                conn.Table<CategoryEntity>().Delete(c => c.DbId == categoryId);
            });
        }

        public async Task DeleteAllAsync()
        {

            await _db.DeleteAllAsync<ArticleEntity>();
            await _db.DeleteAllAsync<CardEntity>();
            await _db.DeleteAllAsync<QuizBlockEntity>();
            await _db.DeleteAllAsync<CategoryEntity>();
            await _db.DeleteAllAsync<DailyActivityEntity>();

            await _db.CreateTableAsync<ArticleEntity>();
            await _db.CreateTableAsync<CardEntity>();
            await _db.CreateTableAsync<QuizBlockEntity>();
            await _db.CreateTableAsync<CategoryEntity>();
            await _db.CreateTableAsync<DailyActivityEntity>();

        }
    }
}