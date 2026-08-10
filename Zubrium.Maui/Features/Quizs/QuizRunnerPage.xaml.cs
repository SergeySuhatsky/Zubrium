using Zubrium.Maui.Attributes;

namespace Zubrium.Maui.Features.Quizs;

[ShellRoute]
public partial class QuizRunnerPage : ContentPage
{
    public QuizRunnerPage(QuizRunnerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
