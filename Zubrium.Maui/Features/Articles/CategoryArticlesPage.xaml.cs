using Zubrium.Maui.Attributes;

namespace Zubrium.Maui.Features.Articles;

[ShellRoute]
public partial class CategoryArticlesPage : ContentPage
{
    public CategoryArticlesPage(CategoryArticlesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
