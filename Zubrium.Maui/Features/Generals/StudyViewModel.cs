using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zubrium.Content.Repository;
using Zubrium.Maui.ViewModels;
using Zubrium.Maui.Features.Study;

namespace Zubrium.Maui.Features.Generals
{
    public partial class StudyViewModel : BaseViewModel
    {
        [ObservableProperty]
        public partial List<string> SelectedCategoryIds { get; set; } = new();

        [ObservableProperty]
        public partial string SelectedCategoryName { get; set; } = "Все категории";

        public StudyViewModel(IContentRepository repository) : base(repository)
        {
        }

        public override void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            // Ловим список категорий
            if (query.TryGetValue("SelectedCategories", out var catsObj) && catsObj is List<CategoryDraft> categories)
            {
                if (categories.Count == 0)
                {
                    SelectedCategoryIds = new List<string>();
                    SelectedCategoryName = "Все категории";
                }
                else
                {
                    SelectedCategoryIds = categories.Select(c => c.Id!).ToList();
                    SelectedCategoryName = string.Join(", ", categories.Select(c => c.Name));
                }
            }
        }

        [RelayCommand]
        public async Task SelectCategory()
        {
            var navParams = new Dictionary<string, object>
            {
                { "IsMultiSelect", true },
                { "PreSelectedIds", SelectedCategoryIds } // Передаем, чтобы галочки остались
            };
            await Shell.Current.GoToAsync(nameof(CategorySelectionPage), navParams);
        }

        [RelayCommand]
        public async Task TakeQuiz() => await Shell.Current.GoToAsync(nameof(Quizs.PickQuizPage));

        [RelayCommand]
        public async Task ReadArticle() => await Shell.Current.GoToAsync(nameof(Articles.PickArticlePage));

        [RelayCommand]
        public async Task LearnNewWords() => await StartSession(StudyMode.NewCards);

        [RelayCommand]
        public async Task RepeatWords() => await StartSession(StudyMode.Review);

        [RelayCommand]
        public async Task MixedMode() => await StartSession(StudyMode.Mixed);

        private async Task StartSession(StudyMode mode)
        {
            await Shell.Current.GoToAsync(nameof(Study.StudySessionPage), new Dictionary<string, object>
            {
                { "CategoryIds", SelectedCategoryIds }, // Передаем List<string>
                { "StudyMode", mode }
            });
        }
    }
}
