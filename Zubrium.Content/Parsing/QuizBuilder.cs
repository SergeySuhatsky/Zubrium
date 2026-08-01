using Markdig.Extensions.CustomContainers;
using Markdig.Renderers.Html;
using System;
using System.Collections.Generic;
using System.Text;
using Zubrium.Domain;

namespace Zubrium.Content.Parsing
{
    public static class QuizBuilder
    {
        public static QuizBlock FromContainer(CustomContainer block, string rawSource)
        {
            // 1. Извлекаем заголовок квиза
            var attributes = block.GetAttributes();
            string? title = attributes.Properties?.FirstOrDefault(p => p.Key == "title").Value;

            // 2. Достаем внутренний текст контейнера
            var span = block.Span;
            string fullText = rawSource.Substring(span.Start, span.Length);
            string innerText = ExtractInnerMarkdown(fullText);

            var questions = new List<QuizQuestion>();

            // 3. Разбиваем блок на отдельные вопросы по разделителю "---" 
            var questionBlocks = innerText.Split(new[] { "\n---\n", "\r\n---\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var qBlock in questionBlocks)
            {
                string questionContent = qBlock.Trim();
                string? explanation = null;

                // 4. Отделяем блок объяснения
                var explanationSplit = questionContent.Split(new[] { "--- explanation" }, StringSplitOptions.None);
                if (explanationSplit.Length > 1)
                {
                    questionContent = explanationSplit[0].Trim();
                    explanation = explanationSplit[1].Trim();
                }

                // 5. Парсим сам вопрос и варианты ответов
                var (questionMarkdown, options) = ParseQuestionAndOptions(questionContent);

                questions.Add(new QuizQuestion(questionMarkdown, options, explanation));
            }

            return new QuizBlock(title, questions);
        }

        private static (string Question, List<AnswerOption> Options) ParseQuestionAndOptions(string content)
        {
            var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            var questionLines = new List<string>();
            var options = new List<AnswerOption>();

            foreach (var line in lines)
            {
                string trimmedLine = line.TrimStart();

                // Проверяем, является ли строка вариантом ответа (поддерживаем [x] и [X])
                if (trimmedLine.StartsWith("- [ ]") || trimmedLine.StartsWith("- [x]", StringComparison.OrdinalIgnoreCase))
                {
                    bool isCorrect = trimmedLine.StartsWith("- [x]", StringComparison.OrdinalIgnoreCase);

                    // Вырезаем сам текст ответа (пропускаем "- [ ] " или "- [x] ", это 6 символов)
                    string optionText = trimmedLine.Length >= 6 ? trimmedLine.Substring(6).Trim() : string.Empty;

                    options.Add(new AnswerOption(optionText, isCorrect));
                }
                else
                {
                    // Если мы ещё не встретили варианты ответов, то это часть самого вопроса
                    if (options.Count == 0)
                    {
                        questionLines.Add(line);
                    }
                    else if (!string.IsNullOrWhiteSpace(line))
                    {
                        // Если попался текст после начала опций (например, многострочный ответ),
                        // приклеиваем его к последнему варианту ответа.
                        var lastOption = options.Last();
                        lastOption.TextMarkdown += "\n" + line.Trim();
                    }
                }
            }

            string questionMarkdown = string.Join("\n", questionLines).Trim();
            return (questionMarkdown, options);
        }

        private static string ExtractInnerMarkdown(string containerText)
        {
            var lines = containerText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            if (lines.Length >= 2)
            {
                // Пропускаем первую строку ::: quiz и последнюю :::
                return string.Join("\n", lines.Skip(1).Take(lines.Length - 2)).Trim();
            }
            return containerText;
        }
    }
}
