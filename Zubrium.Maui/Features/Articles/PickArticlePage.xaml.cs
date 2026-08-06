using Zubrium.Maui.Attributes;

namespace Zubrium.Maui.Features.Articles;

[ShellRoute]
public partial class PickArticlePage : ContentPage
{
    public PickArticlePage(PickArticleViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is PickArticleViewModel vm)
        {
            vm.LoadCategoriesCommand.Execute(null);
        }
    }
}
