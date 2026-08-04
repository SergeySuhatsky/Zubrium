using Zubrium.Maui.ViewModels;

namespace Zubrium.Maui.Features.Cards;

public partial class DecksPage : ContentPage
{
    public DecksPage(DecksViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is DecksViewModel vm)
        {
            vm.LoadCategoriesCommand.Execute(null);
        }
    }
}