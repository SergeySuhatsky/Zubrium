using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using Zubrium.Content.Repository;
using Zubrium.Domain;
using Zubrium.Maui.ViewModels;
using Zubrium.Persistence.Mappers;

namespace Zubrium.Maui.Features.Quizs
{
    public partial class CategoryQuizzesViewModel : BaseViewModel
    {
        [ObservableProperty]
        public partial ObservableCollection<QuizBlock> Quizzes { get; set; } = new();

        [ObservableProperty]
        public partial string CategoryName { get; set; } = string.Empty;

        public CategoryQuizzesViewModel(IContentRepository repository) : base(repository)
        {
        }

        public override async void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("CategoryId", out var catIdObj) && catIdObj is string catId)
            {
                var categories = await _repository.GetAllCategoriesAsync();
                CategoryName = categories.FirstOrDefault(c => c.DbId == catId)?.Name ?? "Категория";

                var quizEntities = await _repository.GetQuizBlocksByCategoryAsync(catId);
                Quizzes.Clear();
                foreach (var q in quizEntities)
                {
                    Quizzes.Add(q.ToDomain());
                }
            }
        }

        [RelayCommand]
        public async Task StartQuiz(string quizId)
        {
            await Shell.Current.GoToAsync(nameof(QuizRunnerPage), new Dictionary<string, object>
            {
                { "QuizId", quizId }
            });
        }
    }
}
