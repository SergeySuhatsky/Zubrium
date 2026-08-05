using CommunityToolkit.Mvvm.ComponentModel;
using Zubrium.Domain;

namespace Zubrium.Maui.Features.Quizs
{
    public partial class QuizOptionViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private bool _isAnswered;

        public string TextMarkdown { get; }
        public bool IsCorrectOption { get; }
        public bool IsSingleChoice { get; }

        public QuizOptionViewModel(AnswerOption option, bool isSingleChoice)
        {
            TextMarkdown = option.TextMarkdown;
            IsCorrectOption = option.IsCorrect;
            IsSingleChoice = isSingleChoice;
        }
    }
}
