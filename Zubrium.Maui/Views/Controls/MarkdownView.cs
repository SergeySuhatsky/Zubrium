using Microsoft.Maui.Controls;
using System.Threading;
using System.Threading.Tasks;
using Zubrium.Maui.Services.MarkdownRender;

namespace Zubrium.Maui.Views.Controls
{
    /// <summary>
    /// Легковесный UI-контрол для отображения Markdown.
    /// Делегирует всю тяжелую работу синглтон-сервису IMarkdownRenderService.
    /// </summary>
    public class MarkdownView : ContentView
    {
        private IMarkdownRenderService? _renderService;
        private CancellationTokenSource? _renderCts;

        // Внутренние элементы для отображения загрузки и контента
        private readonly Grid _rootGrid;
        private readonly ActivityIndicator _loadingIndicator;
        private readonly ContentView _markdownContainer;

        #region Bindable Properties

        public static readonly BindableProperty TextProperty = BindableProperty.Create(
            nameof(Text), typeof(string), typeof(MarkdownView), string.Empty,
            propertyChanged: OnRenderPropertyChanged);

        public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
            nameof(TextColor), typeof(Color), typeof(MarkdownView), Colors.Black,
            propertyChanged: OnRenderPropertyChanged);

        public static readonly BindableProperty LatexTextColorProperty = BindableProperty.Create(
            nameof(LatexTextColor), typeof(Color), typeof(MarkdownView), Colors.Black,
            propertyChanged: OnRenderPropertyChanged);

        public static readonly BindableProperty BaseFontSizeProperty = BindableProperty.Create(
            nameof(BaseFontSize), typeof(double), typeof(MarkdownView), 14.0,
            propertyChanged: OnRenderPropertyChanged);

        public static readonly BindableProperty LatexFontSizeProperty = BindableProperty.Create(
            nameof(LatexFontSize), typeof(double), typeof(MarkdownView), 14.0,
            propertyChanged: OnRenderPropertyChanged);

        public static readonly BindableProperty CodeBackgroundColorProperty = BindableProperty.Create(
            nameof(CodeBackgroundColor), typeof(Color), typeof(MarkdownView), Color.FromArgb("#F6F8FA"),
            propertyChanged: OnRenderPropertyChanged);

        public static readonly BindableProperty QuoteBarColorProperty = BindableProperty.Create(
            nameof(QuoteBarColor), typeof(Color), typeof(MarkdownView), Color.FromArgb("#D0D7DE"),
            propertyChanged: OnRenderPropertyChanged);

        public static readonly BindableProperty FontFamilyProperty = BindableProperty.Create(
            nameof(FontFamily), typeof(string), typeof(MarkdownView), "Segoe UI",
            propertyChanged: OnRenderPropertyChanged);

        #endregion

        #region CLR Properties

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public Color TextColor
        {
            get => (Color)GetValue(TextColorProperty);
            set => SetValue(TextColorProperty, value);
        }

        public Color LatexTextColor
        {
            get => (Color)GetValue(LatexTextColorProperty);
            set => SetValue(LatexTextColorProperty, value);
        }

        public double BaseFontSize
        {
            get => (double)GetValue(BaseFontSizeProperty);
            set => SetValue(BaseFontSizeProperty, value);
        }

        public double LatexFontSize
        {
            get => (double)GetValue(LatexFontSizeProperty);
            set => SetValue(LatexFontSizeProperty, value);
        }

        public Color CodeBackgroundColor
        {
            get => (Color)GetValue(CodeBackgroundColorProperty);
            set => SetValue(CodeBackgroundColorProperty, value);
        }

        public Color QuoteBarColor
        {
            get => (Color)GetValue(QuoteBarColorProperty);
            set => SetValue(QuoteBarColorProperty, value);
        }

        public string FontFamily
        {
            get => (string)GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        #endregion

        public MarkdownView()
        {
            // Устанавливаем базовые отступы, если нужно
            Padding = new Thickness(0);

            // Создаем спиннер загрузки
            _loadingIndicator = new ActivityIndicator
            {
                IsRunning = false,
                IsVisible = false,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(10),
                WidthRequest = 24,
                HeightRequest = 24
            };

            // Привязываем цвет спиннера к цвету текста компонента
            _loadingIndicator.SetBinding(ActivityIndicator.ColorProperty, new Binding(nameof(TextColor), source: this));

            // Контейнер, в который будет помещаться отрендеренный Markdown
            _markdownContainer = new ContentView();

            // Сетка, накладывающая спиннер поверх текста
            _rootGrid = new Grid();
            _rootGrid.Children.Add(_markdownContainer);
            _rootGrid.Children.Add(_loadingIndicator);

            // Устанавливаем сетку как единственный корневой элемент контрола
            this.Content = _rootGrid;
        }

        /// <summary>
        /// Универсальный обработчик изменения любого свойства, влияющего на внешний вид
        /// </summary>
        private static void OnRenderPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is MarkdownView view)
            {
                view.InvalidateRender();
            }
        }

        /// <summary>
        /// Отменяет предыдущий незаконченный рендер и планирует новый с небольшой задержкой (Debounce).
        /// Это предотвращает зависание UI при потоковом вводе текста или одновременной смене нескольких свойств.
        /// </summary>
        private void InvalidateRender()
        {
            _renderCts?.Cancel();
            _renderCts = new CancellationTokenSource();
            var token = _renderCts.Token;

            // Ждем 100мс в фоне (Debounce)
            Task.Run(async () =>
            {
                await Task.Delay(100, token);

                if (token.IsCancellationRequested) return;

                // ВАЖНО: Переключаемся на ГЛАВНЫЙ ПОТОК ДО того, как начнем создавать UI элементы!
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    if (token.IsCancellationRequested) return;
                    await PerformRenderAsync(token);
                });
            }, token);
        }

        private async Task PerformRenderAsync(CancellationToken token)
        {
            EnsureServiceResolved();

            if (_renderService == null)
                return;

            var currentText = Text;

            if (string.IsNullOrWhiteSpace(currentText))
            {
                _markdownContainer.Content = null;
                _loadingIndicator.IsRunning = false;
                _loadingIndicator.IsVisible = false;
                return;
            }

            // Так как мы уже в MainThread, можно менять UI напрямую
            _loadingIndicator.IsVisible = true;
            _loadingIndicator.IsRunning = true;
            _markdownContainer.Opacity = 0.5;

            var options = new MarkdownRenderOptions
            {
                TextColor = this.TextColor,
                LatexTextColor = this.LatexTextColor,
                BaseFontSize = this.BaseFontSize,
                LatexFontSize = this.LatexFontSize,
                CodeBackgroundColor = this.CodeBackgroundColor,
                QuoteBarColor = this.QuoteBarColor,
                FontFamily = this.FontFamily
            };

            // Вызываем рендер. Сервис будет собирать UI в главном потоке,
            // но тяжелые формулы (CreateLatexViewAsync) будут сами "уходить" в фон благодаря Task.Run внутри сервиса.
            var renderedView = await _renderService.RenderToViewAsync(currentText, options, token);

            if (!token.IsCancellationRequested)
            {
                _markdownContainer.Content = renderedView;
                _markdownContainer.Opacity = 1.0;
                _loadingIndicator.IsRunning = false;
                _loadingIndicator.IsVisible = false;
            }
        }

        private void EnsureServiceResolved()
        {
            if (_renderService != null) return;

            // Безопасный способ получения сервиса из MAUI DI в рантайме
#if WINDOWS || MACCATALYST || IOS || ANDROID
            _renderService = Application.Current?.Handler?.MauiContext?.Services.GetService<IMarkdownRenderService>()
                             ?? IPlatformApplication.Current?.Services.GetService<IMarkdownRenderService>();
#endif

            // Если DI по какой-то причине недоступен (например, в превьювере Visual Studio)
            // создаем фоллбэк-экземпляр
            if (_renderService == null)
            {
                System.Diagnostics.Debug.WriteLine("[MarkdownView] Предупреждение: IMarkdownRenderService не найден в DI контейнере. Используется запасной экземпляр.");
                _renderService = new MarkdownRenderService();
            }
        }
    }
}