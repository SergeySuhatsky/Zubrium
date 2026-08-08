namespace Zubrium.Maui.Features.Quizs;

public partial class QuizzesPage : ContentPage
{
	public QuizzesPage(QuizzesViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;

    }

    // Этот метод вызывается каждый раз, когда страница появляется на экране
    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is QuizzesViewModel vm)
        {
            // Вызываем сгенерированную команду
            vm.LoadCategoriesCommand.Execute(null);
        }
    }
}