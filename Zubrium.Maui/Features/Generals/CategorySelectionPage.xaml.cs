using Zubrium.Maui.Attributes;

namespace Zubrium.Maui.Features.Generals;

[ShellRoute]
public partial class CategorySelectionPage : ContentPage
{
    public CategorySelectionPage(CategorySelectionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
