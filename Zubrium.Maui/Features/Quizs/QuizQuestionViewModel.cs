using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using Zubrium.Domain;

namespace Zubrium.Maui.Features.Quizs
{
    public partial class QuizQuestionViewModel : ObservableObject
    {
        [ObservableProperty]
        public partial string QuestionMarkdown { get; set; } = string.Empty;

        [ObservableProperty]
        public partial ObservableCollection<QuizOptionViewModel> Options { get; set; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsSubmitButtonVisible))]
        public partial bool IsAnswered { get; set; }

        [ObservableProperty]
        public partial bool HasExplanation { get; set; }

        [ObservableProperty]
        public partial string ExplanationMarkdown { get; set; } = string.Empty;

        public bool IsSingleChoice { get; }

        public bool IsSubmitButtonVisible => !IsSingleChoice && !IsAnswered;

        private readonly QuizQuestion _domainQuestion;

        public QuizQuestionViewModel(QuizQuestion question)
        {
            _domainQuestion = question;
            QuestionMarkdown = question.QuestionMarkdown;
            IsSingleChoice = question.Options.Count(o => o.IsCorrect) == 1;

            foreach (var opt in question.Options)
            {
                Options.Add(new QuizOptionViewModel(opt, IsSingleChoice));
            }
        }

        [RelayCommand]
        public void ToggleOption(QuizOptionViewModel selected)
        {
            if (IsAnswered || selected == null)
                return;

            if (IsSingleChoice)
            {
                foreach (var opt in Options)
                {
                    opt.IsSelected = false;
                }

                selected.IsSelected = true;
                SubmitAnswer();
            }
            else
            {
                selected.IsSelected = !selected.IsSelected;
            }
        }

        [RelayCommand]
        public void SubmitAnswer()
        {
            if (IsAnswered)
                return;

            IsAnswered = true;

            foreach (var opt in Options)
            {
                opt.IsAnswered = true;
            }

            if (!string.IsNullOrWhiteSpace(_domainQuestion.ExplanationMarkdown))
            {
                HasExplanation = true;
                ExplanationMarkdown = _domainQuestion.ExplanationMarkdown;
            }
        }
    }
}
