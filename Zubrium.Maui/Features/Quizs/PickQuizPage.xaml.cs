using Zubrium.Maui.Attributes;

namespace Zubrium.Maui.Features.Quizs;

[ShellRoute]
public partial class PickQuizPage : ContentPage
{
    public PickQuizPage(PickQuizViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is PickQuizViewModel vm)
        {
            vm.LoadCategoriesCommand.Execute(null);
        }
    }
}