using System;
using System.Collections.Generic;
using System.Text;
using Zubrium.Domain;
using Zubrium.Persistence.Entities;

namespace Zubrium.Content.Repository
{
    public interface IContentRepository
    {
        

        // --- Статьи (Articles) ---
        Task<ArticleEntity> GetArticleAsync(string articleId);
        Task<List<ArticleEntity>> GetArticlesAsync();
        Task<List<ArticleEntity>> GetArticlesByCategoryAsync(string categoryId);
        Task SaveArticlesAsync(IEnumerable<ArticleEntity> articles);
        Task<int> SaveArticleAsync(ArticleEntity article);
        Task DeleteArticleAsync(string articleId);

        // --- Карточки (Cards) ---
        Task<CardEntity> GetCardAsync(string cardId);
        Task<List<CardEntity>> GetCardsAsync();
        Task<List<CardEntity>> GetCardsByCategoryAsync(string categoryId);
        Task<int> SaveCardAsync(CardEntity card);
        Task SaveCardsAsync(IEnumerable<CardEntity> cards);
        Task DeleteCardAsync(string cardId);

        // --- Квизы (Quizzes) ---
        Task<QuizBlockEntity> GetQuizBlockAsync(string quizId);
        Task<List<QuizBlockEntity>> GetQuizBlocksAsync();
        Task<List<QuizBlockEntity>> GetQuizBlocksByCategoryAsync(string categoryId);
        Task<int> SaveQuizBlockAsync(QuizBlockEntity quiz);
        Task SaveQuizBlocksAsync(IEnumerable<QuizBlockEntity> quizzes);
        Task DeleteQuizBlockAsync(string quizId);

        // --- Категории (Categories) ---
        Task<CategoryEntity?> GetCategoryByNameAsync(string name);
        Task<List<CategoryEntity>> GetAllCategoriesAsync();
        Task<int> SaveCategoryAsync(CategoryEntity category);
        Task DeleteCategoryAsync(string categoryId);

        // --- Импорт ContentSet ---
        Task InsertContentSet(ParsedContentSet contentSet, bool isArticles = true, bool isQuizes = true, bool isCards = true);

        // --- Активность (Activity) ---
        Task LogDailyActivityAsync(DateTime date, int newCards, int reviewCards);
        Task<List<Persistence.Entities.DailyActivityEntity>> GetDailyActivitiesAsync();
    }
}
