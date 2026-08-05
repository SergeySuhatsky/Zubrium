using Zubrium.Maui.Attributes;

namespace Zubrium.Maui.Features.Quizs;

[ShellRoute]
public partial class CategoryQuizzesPage : ContentPage
{
    public CategoryQuizzesPage(CategoryQuizzesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
