using System;
using System.Collections.Generic;
using System.Text;

namespace Zubrium.Maui.Services.MarkdownRender
{
    public interface IMarkdownRenderService
    {
        /// <summary>
        /// Выполняет предварительную загрузку тяжелых компонентов (Regex, парсеры).
        /// </summary>
        Task WarmUpAsync();

        /// <summary>
        /// Рендерит строку Markdown в готовое визуальное дерево (View).
        /// </summary>
        Task<View> RenderToViewAsync(string markdown, MarkdownRenderOptions options, CancellationToken token=default);

        /// <summary>
        /// Позволяет добавить кастомный шрифт для формул из потока.
        /// </summary>
        void AddMathFont(Stream fontStream);

        /// <summary>
        /// Позволяет добавить кастомный шрифт для формул из файлов приложения.
        /// </summary>
        Task AddMathFontAsync(string appPackageFileName);
    }
}
