using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;
using Zubrium.Content.Repository;
using Zubrium.Maui.ViewModels;

namespace Zubrium.Maui.Features.Articles
{
    public partial class ArticleViewModel : BaseViewModel
    {
        [ObservableProperty]
        public partial string SearchText { get; set; } = string.Empty;

        [ObservableProperty]
        public partial ObservableCollection<CategoryDisplayModel> Categories { get; set; } = new();

        private List<CategoryDisplayModel> _allCategories = new();

        public ArticleViewModel(IContentRepository repository) : base(repository)
        {
        }

        public override async void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            await LoadCategoriesAsync();
        }

        [RelayCommand]
        public async Task LoadCategories()
        {
            await LoadCategoriesAsync();
        }

        private async Task LoadCategoriesAsync()
        {
            var dbCategories = await _repository.GetAllCategoriesAsync();
            _allCategories.Clear();

            var colors = new[] { "#8E95A4", "#66BB6A", "#FFA726", "#42A5F5", "#AB47BC" };
            int colorIndex = 0;

            foreach (var cat in dbCategories)
            {
                var articles = await _repository.GetArticlesByCategoryAsync(cat.DbId);

                if (articles.Count == 0) continue;

                _allCategories.Add(new CategoryDisplayModel
                {
                    CategoryId = cat.DbId,
                    Name = cat.Name,
                    ItemCount = articles.Count,
                    ItemCountText = $"{articles.Count} статей",
                    IconColor = Color.FromArgb(colors[colorIndex % colors.Length])
                });
                colorIndex++;
            }

            FilterCategories();
        }

        partial void OnSearchTextChanged(string value)
        {
            FilterCategories();
        }

        private void FilterCategories()
        {
            Categories.Clear();
            var query = SearchText?.Trim() ?? string.Empty;

            foreach (var cat in _allCategories)
            {
                if (string.IsNullOrEmpty(query) || cat.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    Categories.Add(cat);
                }
            }
        }

        [RelayCommand]
        public async Task OpenCategory(string categoryId)
        {
            await Application.Current.MainPage.DisplayAlert("Клик", $"Открываем категорию: {categoryId}", "OK");
        }

        [RelayCommand]
        public async Task Import()
        {
            await Shell.Current.GoToAsync(nameof(Generals.ImportPage));
        }
    }
}
