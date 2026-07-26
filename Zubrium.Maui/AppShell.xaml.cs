using System.Reflection;
using Zubrium.Maui.Attributes;

namespace Zubrium.Maui
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            RegisterRoutesAutomated();
        }
    

    private void RegisterRoutesAutomated()
        {
            // Получаем все классы в текущей сборке
            var pagesWithRoute = Assembly.GetExecutingAssembly().GetTypes()
                // Оставляем только те, что наследуются от ContentPage (не абстрактные)
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(ContentPage)))
                // Ищем только те, у которых есть наш атрибут [ShellRoute]
                .Where(t => t.GetCustomAttribute<ShellRouteAttribute>() != null);

            foreach (var pageType in pagesWithRoute)
            {
                // Регистрируем маршрут. pageType.Name вернет название класса (например, "ArticleDetailsPage")
                Routing.RegisterRoute(pageType.Name, pageType);
            }
        }
    }
}
