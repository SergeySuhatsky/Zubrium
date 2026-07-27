using Zubrium.Maui.Attributes;

namespace Zubrium.Maui.Features.Generals;

[ShellRoute]
public partial class ImportPage : ContentPage
{
	public ImportPage(ImportViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;

    }
}