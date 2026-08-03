using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Zubrium.Maui.Services.MarkdownRender;
using Zubrium.Content;
using Zubrium.Maui.ViewModels;
using Zubrium.Maui.Features.Articles;
using MauiIcons.Material;
using Zubrium.Maui.Features.Quizs;
using Microsoft.Maui.Controls;
using System.Reflection;
using Zubrium.Content.Repository;
using Zubrium.Content.Parsing;

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

            // Register markdown render service
            builder.Services.AddSingleton<IMarkdownRenderService, MarkdownRenderService>();
            // Register markdown parser service
            builder.Services.AddSingleton<IDeckSourceParser, DeckSourceParser>();

            // Register SQLite content repository
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "ZubriumData.db3");
            builder.Services.AddSingleton<IContentRepository>(s => new SqliteContentRepository(dbPath));


            // Register pages and view models using reflection
            var assembly = Assembly.GetExecutingAssembly();

            var viewModels = assembly.GetTypes()
                .Where(t => t.IsClass
                            && !t.IsAbstract
                            && t.IsSubclassOf(typeof(BaseViewModel)));
            foreach (var viewModel in viewModels)
            {
                builder.Services.AddTransient(viewModel);
            }

            var pages = assembly.GetTypes()
                .Where(t => t.IsClass
                            && !t.IsAbstract
                            && t.IsSubclassOf(typeof(ContentPage)));

            foreach (var page in pages)
            {
                builder.Services.AddTransient(page);
            }
                

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
