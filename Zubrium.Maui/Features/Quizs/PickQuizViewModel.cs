using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;
using System.Linq;
using Zubrium.Content.Repository;
using Zubrium.Maui.ViewModels;

namespace Zubrium.Maui.Features.Quizs
{
    public partial class PickQuizViewModel : BaseViewModel
    {
        [ObservableProperty]
        public partial ObservableCollection<CategoryDisplayModel> Categories { get; set; } = new();

        public PickQuizViewModel(IContentRepository repository) : base(repository)
        {
        }

        public override void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            // Данные загружаются при появлении страницы через LoadCategoriesCommand.
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
                var quizzes = await _repository.GetQuizBlocksByCategoryAsync(cat.DbId);
                if (quizzes.Count == 0) continue;

                Categories.Add(new CategoryDisplayModel
                {
                    CategoryId = cat.DbId,
                    Name = cat.Name,
                    ItemCount = quizzes.Count,
                    ItemCountText = $"{quizzes.Count} квизов",
                    IconColor = Color.FromArgb(colors[colorIndex % colors.Length])
                });
                colorIndex++;
            }
        }

        [RelayCommand]
        public async Task SelectCategory(string categoryId)
        {
            await Shell.Current.GoToAsync(nameof(CategoryQuizzesPage), new Dictionary<string, object>
            {
                { "CategoryId", categoryId }
            });
        }
    }
}
