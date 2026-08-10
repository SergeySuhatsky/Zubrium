using Zubrium.Maui.Attributes;

namespace Zubrium.Maui.Features.Articles;

[ShellRoute]
public partial class ArticleReaderPage : ContentPage
{
    public ArticleReaderPage(ArticleReaderViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
