using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Zubrium.Maui.Services.MarkdownRender;
using Zubrium.Content;
using Zubrium.Maui.ViewModels;
using Zubrium.Maui.Features.Articles;
using MauiIcons.Material;
using Zubrium.Maui.Features.Settings;
using Zubrium.Maui.Features.Quizs;
using Zubrium.Maui.Features.Cards;

namespace Zubrium.Maui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseMaterialMauiIcons()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
            builder.Services.AddSingleton<IMarkdownRenderService, MarkdownRenderService>();

            builder.Services.AddTransient<ArticlePage>();
            builder.Services.AddTransient<ArticleViewModel>();

            builder.Services.AddTransient<SettingsMenuPage>();
            builder.Services.AddTransient<SettingsMenuViewModel>();

            builder.Services.AddTransient<QuizzePage>();
            builder.Services.AddTransient<QuizzesViewModel>();

            builder.Services.AddTransient<CardsPage>();
            builder.Services.AddTransient<CardsViewModel>();

            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "ZubriumData.db3");
            builder.Services.AddSingleton<IContentRepository>(s => new SqliteContentRepository(dbPath));
#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
