using Zubrium.Maui.Attributes;

namespace Zubrium.Maui.Features.Generals;

[ShellRoute]
public partial class CategoryContentPage : ContentPage
{
    public CategoryContentPage(CategoryContentViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private void OnScrollViewScrolled(object sender, ScrolledEventArgs e)
    {
        if (sender is ScrollView scrollView && BindingContext is CategoryContentViewModel vm)
        {
            if (scrollView.ScrollY >= scrollView.ContentSize.Height - scrollView.Height - 200)
            {
                vm.LoadNextChunkCommand.Execute(null);
            }
        }
    }
}
