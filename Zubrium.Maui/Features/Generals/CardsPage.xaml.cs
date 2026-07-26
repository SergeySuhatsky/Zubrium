using Zubrium.Maui.Services.MarkdownRender;

namespace Zubrium.Maui.Features.Generals;

public partial class CardsPage : ContentPage
{
    public CardsPage(CardsViewModel viewModel)
    {

        InitializeComponent();
        BindingContext = viewModel;

    }

}