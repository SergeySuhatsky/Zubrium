using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Zubrium.Content.Repository;
using Zubrium.Domain;
using Zubrium.Maui.ViewModels;
using Zubrium.Persistence.Mappers;

namespace Zubrium.Maui.Features.Quizs
{
    public partial class QuizRunnerViewModel : BaseViewModel
    {
        [ObservableProperty]
        public partial string QuizTitle { get; set; } = string.Empty;

        [ObservableProperty]
        public partial ObservableCollection<QuizQuestionViewModel> Questions { get; set; } = new();

        public QuizRunnerViewModel(IContentRepository repository) : base(repository)
        {
        }

        public override async void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("QuizId", out var idObj) && idObj is string quizId)
            {
                var quizEntity = await _repository.GetQuizBlockAsync(quizId);
                if (quizEntity != null)
                {
                    var quiz = quizEntity.ToDomain();
                    QuizTitle = quiz.Title;
                    Questions.Clear();

                    foreach (var question in quiz.Questions)
                    {
                        Questions.Add(new QuizQuestionViewModel(question));
                    }
                }
            }
        }

        [RelayCommand]
        public async Task FinishQuiz()
        {
            await GoBack();
        }
    }
}
