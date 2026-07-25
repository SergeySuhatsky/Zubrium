using Microsoft.Extensions.Logging;
using Zubrium.Maui.Services.MarkdownRender;
using Zubrium.Content;

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
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
            builder.Services.AddSingleton<IMarkdownRenderService, MarkdownRenderService>();
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "ZubriumData.db3");
            builder.Services.AddSingleton<IContentRepository>(s => new SqliteContentRepository(dbPath));
#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
