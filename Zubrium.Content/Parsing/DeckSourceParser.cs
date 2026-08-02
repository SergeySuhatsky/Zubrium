using Markdig;
using Markdig.Extensions.CustomContainers;
using Markdig.Extensions.Yaml;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using System;
using System.Collections.Generic;
using System.Text;
using Zubrium.Domain;

namespace Zubrium.Content.Parsing
{
    public class DeckSourceParser
    {
        public ParsedContentSet Parse(string rawMarkdown)
        {
            var pipeline = ContentMarkdownPipeline.Build();
            var document = Markdown.Parse(rawMarkdown, pipeline);
            var result = new ParsedContentSet();

            // 1. ИЗВЛЕКАЕМ YAML FRONTMATTER
            var yamlBlock = document.Descendants<YamlFrontMatterBlock>().FirstOrDefault();
            if (yamlBlock != null)
            {
                // Markdig хранит строки YAML в свойстве Lines
                var yamlLines = yamlBlock.Lines.Lines
                    .Take(yamlBlock.Lines.Count)
                    .Select(l => l.ToString())
                    .ToList();

                // Простой парсинг "ключ: значение"
                foreach (var line in yamlLines)
                {
                    if (line.StartsWith("category:"))
                        result.CategoryHint = line.Replace("category:", "").Trim();
                }
            }

            // 2. ИЗВЛЕКАЕМ КОНТЕЙНЕРЫ
            var currentDeck = new Deck { Id = "default-deck", Title = "Default Deck", Cards = new List<Card>() };

            foreach (var container in document.Descendants<CustomContainer>())
            {
                if (container.Info == "card")
                {
                    var card = CardBuilder.FromContainer(container, rawMarkdown);
                    result.Cards.Add(card);
                    Console.WriteLine($"[Карточка] Успешно распарсена: {card.Id}");
                }
                else if (container.Info == "quiz")
                {
                    var quiz = QuizBuilder.FromContainer(container, rawMarkdown);
                    result.Quizzes.Add(quiz);
                    Console.WriteLine($"[Квиз] Успешно распарсен: {quiz.Title ?? "Без названия"}");
                }
                else if (container.Info == "article")
                {
                    var article = ArticleBuilder.FromContainer(container, rawMarkdown);
                    result.Articles.Add(article);
                    Console.WriteLine($"[Статья] Успешно распарсена: {article.Title}");
                }
            }

            return result;
        }
    }
}

