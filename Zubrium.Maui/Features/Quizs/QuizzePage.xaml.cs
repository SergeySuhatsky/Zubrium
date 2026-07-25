namespace Zubrium.Maui.Features.Quizs;

public partial class QuizzePage : ContentPage
{
	public QuizzePage(QuizzesViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;

    }
}