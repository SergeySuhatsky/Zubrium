using Plugin.Maui.SwipeCardView.Core;
using Zubrium.Maui.Attributes;

namespace Zubrium.Maui.Features.Study;

[ShellRoute]
public partial class StudySessionPage : ContentPage
{
    public StudySessionPage(StudySessionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    // Программно вызываем свайп карточки плагина при нажатии на кнопки внизу[cite: 2]
    private async void OnSwipeLeftClicked(object sender, EventArgs e)
    {
        await CardSwipeView.InvokeSwipe(SwipeCardDirection.Left);
    }

    private async void OnSwipeRightClicked(object sender, EventArgs e)
    {
        await CardSwipeView.InvokeSwipe(SwipeCardDirection.Right);
    }
}