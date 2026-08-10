using Plugin.Maui.SwipeCardView.Core;
using Zubrium.Maui.Attributes;

namespace Zubrium.Maui.Features.Study;

[ShellRoute]
public partial class StudySessionPage : ContentPage
{
    private bool _isSwiping = false;

    public StudySessionPage(StudySessionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnSwipeLeftClicked(object sender, EventArgs e)
    {
        if (_isSwiping) return;
        _isSwiping = true;
        await CardSwipeView.InvokeSwipe(SwipeCardDirection.Left);
        _isSwiping = false;
    }

    private async void OnSwipeRightClicked(object sender, EventArgs e)
    {
        if (_isSwiping) return;
        _isSwiping = true;
        await CardSwipeView.InvokeSwipe(SwipeCardDirection.Right);
        _isSwiping = false;
    }
}