using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using Zubrium.Content.Repository;
using Zubrium.Domain;
using Zubrium.Maui.ViewModels;
using Zubrium.Persistence.Mappers;

namespace Zubrium.Maui.Features.Articles
{
    public partial class CategoryArticlesViewModel : BaseViewModel
    {
        [ObservableProperty]
        public partial ObservableCollection<Article> Articles { get; set; } = new();

        [ObservableProperty]
        public partial string CategoryName { get; set; } = string.Empty;

        public CategoryArticlesViewModel(IContentRepository repository) : base(repository)
        {
        }

        public override async void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("CategoryId", out var catIdObj) && catIdObj is string catId)
            {
                var categories = await _repository.GetAllCategoriesAsync();
                CategoryName = categories.FirstOrDefault(c => c.DbId == catId)?.Name ?? "Категория";

                var articleEntities = await _repository.GetArticlesByCategoryAsync(catId);
                Articles.Clear();
                foreach (var a in articleEntities)
                {
                    Articles.Add(a.ToDomain());
                }
            }
        }

        [RelayCommand]
        public async Task ReadArticle(string articleId)
        {
            await Shell.Current.GoToAsync(nameof(ArticleReaderPage), new Dictionary<string, object>
            {
                { "ArticleId", articleId }
            });
        }
    }
}
