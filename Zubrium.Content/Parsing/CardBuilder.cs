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

            return new Card(id, frontMarkdown, briefMarkdown, detailedMarkdown);
        }

        private static string ExtractInnerMarkdown(string containerText)
        {
            var lines = containerText.Split('\n');
            // Пропускаем первую строку (::: card) и последнюю (:::)
            if (lines.Length >= 2)
            {
                return string.Join("\n", lines.Skip(1).Take(lines.Length - 2));
            }
            return containerText;
        }
    }
}
