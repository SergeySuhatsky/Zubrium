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
    public partial class QuizRunnerViewModel : BaseViewModel
    {
        private QuizBlock _quiz;
        private int _currentIndex = 0;

        [ObservableProperty] public partial string QuizTitle { get; set; } = string.Empty;
        [ObservableProperty] public partial string ProgressText { get; set; } = string.Empty;
        [ObservableProperty] public partial string QuestionMarkdown { get; set; } = string.Empty;
        [ObservableProperty] public partial ObservableCollection<QuizOptionViewModel> Options { get; set; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNextVisible))]
        [NotifyPropertyChangedFor(nameof(IsFinishVisible))]
        public partial bool IsAnswered { get; set; }

        [ObservableProperty] public partial bool HasExplanation { get; set; }
        [ObservableProperty] public partial string ExplanationMarkdown { get; set; } = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNextVisible))]
        [NotifyPropertyChangedFor(nameof(IsFinishVisible))]
        public partial bool IsLastQuestion { get; set; }

        public bool IsNextVisible => IsAnswered && !IsLastQuestion;
        public bool IsFinishVisible => IsAnswered && IsLastQuestion;

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
                    _quiz = quizEntity.ToDomain();
                    QuizTitle = _quiz.Title;
                    _currentIndex = 0;
                    LoadCurrentQuestion();
                }
            }
        }

        private void LoadCurrentQuestion()
        {
            if (_quiz?.Questions == null || _quiz.Questions.Count == 0) return;

            var currentQ = _quiz.Questions[_currentIndex];
            ProgressText = $"Вопрос {_currentIndex + 1} из {_quiz.Questions.Count}";
            QuestionMarkdown = currentQ.QuestionMarkdown;

            bool isSingleChoice = currentQ.Options.Count(o => o.IsCorrect) == 1;

            Options.Clear();
            foreach (var opt in currentQ.Options)
            {
                Options.Add(new QuizOptionViewModel(opt, isSingleChoice));
            }

            IsAnswered = false;
            HasExplanation = false;
            ExplanationMarkdown = string.Empty;
            IsLastQuestion = _currentIndex == _quiz.Questions.Count - 1;
        }

        [RelayCommand]
        public void ToggleOption(QuizOptionViewModel selected)
        {
            if (IsAnswered || selected == null) return;

            if (selected.IsSingleChoice)
            {
                foreach (var opt in Options)
                {
                    opt.IsSelected = false;
                }
                selected.IsSelected = true;
            }
            else
            {
                selected.IsSelected = !selected.IsSelected;
            }
        }

        [RelayCommand]
        public void SubmitAnswer()
        {
            IsAnswered = true;
            foreach (var opt in Options)
            {
                opt.IsAnswered = true;
            }

            var currentQ = _quiz.Questions[_currentIndex];
            if (!string.IsNullOrWhiteSpace(currentQ.ExplanationMarkdown))
            {
                HasExplanation = true;
                ExplanationMarkdown = currentQ.ExplanationMarkdown;
            }
        }

        [RelayCommand]
        public async Task NextQuestion()
        {
            if (IsLastQuestion)
            {
                await Shell.Current.GoToAsync("../..");
            }
            else
            {
                _currentIndex++;
                LoadCurrentQuestion();
            }
        }
    }
}
