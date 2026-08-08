using Zubrium.Maui.Attributes;

namespace Zubrium.Maui.Features.Study;

[ShellRoute]
public partial class StudySessionPage : ContentPage
{
    public StudySessionPage(StudySessionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
