using Zubrium.Maui.Attributes;

namespace Zubrium.Maui.Features.Study;

[ShellRoute]
public partial class ProgressPage : ContentPage
{
    public ProgressPage(ProgressViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
