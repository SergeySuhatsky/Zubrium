namespace Zubrium.Maui.Features.Settings;

public partial class SettingsMenuPage : ContentPage
{
	public SettingsMenuPage(SettingsMenuViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
    }
}