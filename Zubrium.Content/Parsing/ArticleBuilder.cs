using Markdig.Extensions.CustomContainers;
using Markdig.Renderers.Html;
using System;
using System.Collections.Generic;
using System.Text;
using Zubrium.Domain;

namespace Zubrium.Content.Parsing
{
    public static class ArticleBuilder
    {
        public static Article FromContainer(CustomContainer block, string rawSource)
        {
            // 1. Извлекаем ID из атрибутов
            var attributes = block.GetAttributes();
            string id = attributes.Id ?? Guid.NewGuid().ToString("N");

            // 2. Вырезаем сырой текст статьи по координатам блока
            var span = block.Span;
            string fullText = rawSource.Substring(span.Start, span.Length);
            string bodyMarkdown = ExtractInnerMarkdown(fullText);

            // 3. Пытаемся получить заголовок из атрибутов
            string? title = attributes.Properties?.FirstOrDefault(p => p.Key == "title").Value;

            // Если в атрибутах нет, ищем первый заголовок H1 в тексте
            if (string.IsNullOrWhiteSpace(title))
            {
                var firstH1 = bodyMarkdown
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault(line => line.StartsWith("# "));

                title = firstH1 != null
                    ? firstH1.Substring(2).Trim()
                    : "Без названия";
            }

            return new Article(id, bodyMarkdown, title);
        }

        private static string ExtractInnerMarkdown(string containerText)
        {
            var lines = containerText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            if (lines.Length >= 2)
            {
                return string.Join("\n", lines.Skip(1).Take(lines.Length - 2)).Trim();
            }
            return containerText;
        }
    }
}
