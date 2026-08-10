using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;
using Zubrium.Content.Repository;
using Zubrium.Maui.ViewModels;

namespace Zubrium.Maui.Features.Articles
{
    public partial class PickArticleViewModel : BaseViewModel
    {
        [ObservableProperty]
        public partial ObservableCollection<CategoryDisplayModel> Categories { get; set; } = new();

        public PickArticleViewModel(IContentRepository repository) : base(repository)
        {
        }

        public override void ApplyQueryAttributes(IDictionary<string, object> query)
        {
        }

        [RelayCommand]
        public async Task LoadCategories()
        {
            var dbCategories = await _repository.GetAllCategoriesAsync();
            Categories.Clear();

            var colors = new[] { "#8E95A4", "#66BB6A", "#FFA726", "#42A5F5", "#AB47BC" };
            int colorIndex = 0;

            foreach (var cat in dbCategories)
            {
                // Проверяем наличие статей
                var articles = await _repository.GetArticlesByCategoryAsync(cat.DbId);
                if (articles.Count == 0) continue;

                Categories.Add(new CategoryDisplayModel
                {
                    CategoryId = cat.DbId,
                    Name = cat.Name,
                    ItemCount = articles.Count,
                    ItemCountText = $"{articles.Count} статей",
                    IconColor = Color.FromArgb(colors[colorIndex % colors.Length])
                });
                colorIndex++;
            }
        }

        [RelayCommand]
        public async Task SelectCategory(string categoryId)
        {
            await Shell.Current.GoToAsync(nameof(CategoryArticlesPage), new Dictionary<string, object>
            {
                { "CategoryId", categoryId }
            });
        }
    }
}
