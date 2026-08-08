using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using Zubrium.Content.Repository;
using Zubrium.Domain;
using Zubrium.Maui.ViewModels;
using Zubrium.Persistence.Entities;
using Zubrium.Persistence.Mappers;

namespace Zubrium.Maui.Features.Generals
{
    public partial class CategoryContentViewModel : BaseViewModel
    {
        private string _categoryId = string.Empty;
        private CategoryEntity? _categoryEntity;
        private ContentType _contentType;

        private readonly List<Card> _allCards = new();
        private readonly List<Article> _allArticles = new();
        private readonly List<QuizBlock> _allQuizzes = new();

        private List<Card> _filteredCards = new();
        private List<Article> _filteredArticles = new();
        private List<QuizBlock> _filteredQuizzes = new();

        private int _itemsLoaded;
        private const int ChunkSize = 5;

        [ObservableProperty]
        public partial string CategoryName { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string SearchText { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool IsCardsVisible { get; set; }

        [ObservableProperty]
        public partial bool IsArticlesVisible { get; set; }

        [ObservableProperty]
        public partial bool IsQuizzesVisible { get; set; }

        public ObservableCollection<Card> DisplayCards { get; } = new();
        public ObservableCollection<Article> DisplayArticles { get; } = new();
        public ObservableCollection<QuizBlock> DisplayQuizzes { get; } = new();

        public string PageTitle => _contentType switch
        {
            ContentType.Card => "Карточки",
            ContentType.Article => "Статьи",
            ContentType.Quiz => "Квизы",
            _ => string.Empty
        };

        public CategoryContentViewModel(IContentRepository repository) : base(repository)
        {
        }

        public override async void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("CategoryId", out var categoryIdObj) && categoryIdObj is string categoryId)
            {
                _categoryId = categoryId;
            }

            if (query.TryGetValue("ContentType", out var contentTypeObj))
            {
                if (contentTypeObj is ContentType contentType)
                {
                    _contentType = contentType;
                }
                else if (contentTypeObj is string typeString &&
                         Enum.TryParse<ContentType>(typeString, true, out var parsedType))
                {
                    _contentType = parsedType;
                }
            }

            SetVisibility();
            await LoadCategoryContentAsync();
        }

        private void SetVisibility()
        {
            IsCardsVisible = _contentType == ContentType.Card;
            IsArticlesVisible = _contentType == ContentType.Article;
            IsQuizzesVisible = _contentType == ContentType.Quiz;
        }

        private async Task LoadCategoryContentAsync()
        {
            if (string.IsNullOrWhiteSpace(_categoryId))
            {
                return;
            }

            var categories = await _repository.GetAllCategoriesAsync();
            _categoryEntity = categories.FirstOrDefault(c => c.DbId == _categoryId);
            CategoryName = _categoryEntity?.Name ?? string.Empty;

            _allCards.Clear();
            _allArticles.Clear();
            _allQuizzes.Clear();

            switch (_contentType)
            {
                case ContentType.Card:
                    var cardEntities = await _repository.GetCardsByCategoryAsync(_categoryId);
                    _allCards.AddRange(cardEntities.Select(c => c.ToDomain()));
                    break;
                case ContentType.Article:
                    var articleEntities = await _repository.GetArticlesByCategoryAsync(_categoryId);
                    _allArticles.AddRange(articleEntities.Select(a => a.ToDomain()));
                    break;
                case ContentType.Quiz:
                    var quizEntities = await _repository.GetQuizBlocksByCategoryAsync(_categoryId);
                    _allQuizzes.AddRange(quizEntities.Select(q => q.ToDomain()));
                    break;
            }

            ApplyFilter();
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var query = SearchText?.Trim() ?? string.Empty;
            _itemsLoaded = 0;

            if (_contentType == ContentType.Card)
            {
                _filteredCards = string.IsNullOrEmpty(query)
                    ? _allCards.ToList()
                    : _allCards.Where(c => c.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || c.FrontMarkdown.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || c.BriefMarkdown.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || (c.DetailedMarkdown?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
                        .ToList();
            }
            else if (_contentType == ContentType.Article)
            {
                _filteredArticles = string.IsNullOrEmpty(query)
                    ? _allArticles.ToList()
                    : _allArticles.Where(a => a.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || a.BodyMarkdown.Contains(query, StringComparison.OrdinalIgnoreCase))
                        .ToList();
            }
            else if (_contentType == ContentType.Quiz)
            {
                _filteredQuizzes = string.IsNullOrEmpty(query)
                    ? _allQuizzes.ToList()
                    : _allQuizzes.Where(q => q.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || q.Questions.Any(question => question.QuestionMarkdown.Contains(query, StringComparison.OrdinalIgnoreCase)))
                        .ToList();
            }

            DisplayCards.Clear();
            DisplayArticles.Clear();
            DisplayQuizzes.Clear();
            LoadNextChunk();
        }

        [RelayCommand]
        public void LoadNextChunk()
        {
            if (_contentType == ContentType.Card)
            {
                if (_itemsLoaded >= _filteredCards.Count) return;
                var nextChunk = _filteredCards.Skip(_itemsLoaded).Take(ChunkSize).ToList();
                foreach (var item in nextChunk) DisplayCards.Add(item);
                _itemsLoaded += nextChunk.Count;
                return;
            }

            if (_contentType == ContentType.Article)
            {
                if (_itemsLoaded >= _filteredArticles.Count) return;
                var nextChunk = _filteredArticles.Skip(_itemsLoaded).Take(ChunkSize).ToList();
                foreach (var item in nextChunk) DisplayArticles.Add(item);
                _itemsLoaded += nextChunk.Count;
                return;
            }

            if (_contentType == ContentType.Quiz)
            {
                if (_itemsLoaded >= _filteredQuizzes.Count) return;
                var nextChunk = _filteredQuizzes.Skip(_itemsLoaded).Take(ChunkSize).ToList();
                foreach (var item in nextChunk) DisplayQuizzes.Add(item);
                _itemsLoaded += nextChunk.Count;
            }
        }

        [RelayCommand]
        public async Task SaveCategoryName()
        {
            if (_categoryEntity == null)
            {
                return;
            }

            var trimmed = CategoryName?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed == _categoryEntity.Name)
            {
                return;
            }

            _categoryEntity.Name = trimmed;
            await _repository.SaveCategoryAsync(_categoryEntity);
        }
    }
}
