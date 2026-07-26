namespace Zubrium.Maui.Features.Generals;

public partial class SettingsMenuPage : ContentPage
{
	public SettingsMenuPage(SettingsMenuViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
    }
}