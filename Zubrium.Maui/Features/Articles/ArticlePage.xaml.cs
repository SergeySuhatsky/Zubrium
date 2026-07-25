using Zubrium.Maui.ViewModels;

namespace Zubrium.Maui.Features.Articles;

public partial class ArticlePage : ContentPage
{
	public ArticlePage(ArticleViewModel viewModel)
	{
		InitializeComponent();

        BindingContext = viewModel;
    }
}