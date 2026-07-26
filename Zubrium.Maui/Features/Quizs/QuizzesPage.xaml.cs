namespace Zubrium.Maui.Features.Quizs;

public partial class QuizzesPage : ContentPage
{
	public QuizzesPage(QuizzesViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;

    }
}