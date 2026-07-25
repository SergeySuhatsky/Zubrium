using Zubrium.Maui.Services.MarkdownRender;

namespace Zubrium.Maui.Features.Cards;

public partial class CardsPage : ContentPage
{
    public CardsPage(CardsViewModel viewModel)
    {

        InitializeComponent();
        BindingContext = viewModel;

    }

}