using Microsoft.Extensions.DependencyInjection;

namespace Zubrium.Maui
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            Application.Current.UserAppTheme = AppTheme.Light;

            // Ограничиваем контент только на десктопных платформах (Windows / Mac).
            // На Android и iOS этот код даже не запустится, поэтому ничего не сломает.
            if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
            {
                Application.Current.PageAppearing += (sender, page) =>
                {
                    // Проверяем, что это обычная страница с контентом
                    if (page is ContentPage contentPage && contentPage.Content != null)
                    {
                        // Центрируем корневой элемент страницы и задаем максимальную ширину
                        contentPage.Content.HorizontalOptions = LayoutOptions.Center;
                        contentPage.Content.MaximumWidthRequest = 800;
                    }
                };
            }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}