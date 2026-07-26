using Zubrium.Maui.Attributes;
using Zubrium.Maui.Services.MarkdownRender;

namespace Zubrium.Maui.Features.Generals;


public partial class StudyPage : ContentPage
{
    public StudyPage(StudyViewModel viewModel)
    {

        InitializeComponent();
        BindingContext = viewModel;

    }

}