using Zubrium.Maui.ViewModels;

namespace Zubrium.Maui.Features.Cards;

public partial class DecksPage : ContentPage
{
	public DecksPage(DecksViewModel viewModel)
	{
		InitializeComponent();

        BindingContext = viewModel;
    }
}