using Markdig.Extensions.CustomContainers;
using Markdig.Renderers.Html;
using System;
using System.Collections.Generic;
using System.Text;
using Zubrium.Domain;

namespace Zubrium.Content.Parsing
{
    public static class CardBuilder
    {
        public static Card FromContainer(CustomContainer block, string rawSource)
        {
            // Извлекаем атрибуты контейнера
            var attributes = block.GetAttributes();
            string id = attributes.Id ?? Guid.NewGuid().ToString("N");

            // Получаем весь текст внутри блока
            var span = block.Span;
            string fullText = rawSource.Substring(span.Start, span.Length);

            // Очищаем от обертки ::: card
            string innerText = ExtractInnerMarkdown(fullText);

            string frontMarkdown = "";
            string briefMarkdown = "";
            string? detailedMarkdown = null;

            // Разделяем по маркерам
            var detailedSplit = innerText.Split(new[] { "--- detailed" }, StringSplitOptions.None);
            if (detailedSplit.Length > 1)
            {
                detailedMarkdown = detailedSplit[1].Trim();
            }

            var briefSplit = detailedSplit[0].Split(new[] { "--- brief" }, StringSplitOptions.None);
            frontMarkdown = briefSplit[0].Trim();

            if (briefSplit.Length > 1)
            {
                briefMarkdown = briefSplit[1].Trim();
            }

            // 3. Пытаемся получить заголовок из атрибутов
            string? title = attributes.Properties?.FirstOrDefault(p => p.Key == "title").Value;

            // Если в атрибутах нет, ищем первый заголовок H1 в тексте
            if (string.IsNullOrWhiteSpace(title))
            {
                var firstH1 = frontMarkdown
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault(line => line.StartsWith("# "));

                title = firstH1 != null
                    ? firstH1.Substring(2).Trim()
                    : "Без названия";
            }


            return new Card(id, frontMarkdown, briefMarkdown, title, detailedMarkdown);
        }

        private static string ExtractInnerMarkdown(string containerText)
        {
            // 1. Приводим все переносы к единому виду и убираем пустые строки по краям
            containerText = containerText.Replace("\r\n", "\n").Replace("\r", "\n").Trim();

            var lines = containerText.Split('\n');

            // 2. Теперь мы точно знаем, что lines[0] — это ":::", а последняя строка — это ":::"
            if (lines.Length >= 2)
            {
                // Пропускаем первую строку и берём всё, кроме последней, затем склеиваем
                return string.Join("\n", lines.Skip(1).Take(lines.Length - 2)).Trim();
            }

            return containerText;
        }
    }
}
