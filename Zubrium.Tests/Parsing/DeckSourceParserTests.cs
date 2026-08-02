using System;
using System.Collections.Generic;
using System.Text;
using Zubrium.Content.Parsing;

namespace Zubrium.Tests.Parsing
{
    public class DeckSourceParserTests
    {
        [Fact]
        public void ExtractsCategoryHint_FromYamlFrontMatter()
        {
            var parser = new DeckSourceParser();
            var result = parser.Parse(TestFixtures.FullSampleDeckSource);

            Assert.Equal("Топология 🔢", result.CategoryHint);
        }

        [Fact]
        public void CollectsAllCardsIntoSingleDefaultDeck()
        {
            var parser = new DeckSourceParser();
            var result = parser.Parse(TestFixtures.FullSampleDeckSource);

            Assert.Equal(2, result.Cards.Count);
        }

        [Fact]
        public void ParsesQuizzesSeparatelyFromCards()
        {
            var parser = new DeckSourceParser();
            var result = parser.Parse(TestFixtures.FullSampleDeckSource);

            Assert.Single(result.Quizzes);
            Assert.Equal(2, result.Quizzes[0].Questions.Count);
        }

        [Fact]
        public void ParsesArticles()
        {
            var parser = new DeckSourceParser();
            var result = parser.Parse(TestFixtures.FullSampleDeckSource);

            Assert.Single(result.Articles);
            Assert.Equal("Статья про топологию", result.Articles[0].Title);
        }

        [Fact]
        public void DoesNotAddEmptyDeck_WhenSourceHasNoCards()
        {
            const string noCards = """
                ::: article {title="Статья без карточек"}
                # Статья без карточек
                Текст.
                :::
                """;

            var parser = new DeckSourceParser();
            var result = parser.Parse(noCards);

            Assert.Empty(result.Cards);
        }

        [Fact]
        public void ReturnsEmptyResult_ForPlainMarkdownWithoutContainers()
        {
            const string plain = "# Просто заголовок\n\nПросто абзац без спецблоков.";

            var parser = new DeckSourceParser();
            var result = parser.Parse(plain);

            Assert.Empty(result.Cards);
            Assert.Empty(result.Quizzes);
            Assert.Empty(result.Articles);
            Assert.Null(result.CategoryHint);
        }

        // --- СТАБИЛЬНОСТЬ: содержимое не должно "плавать" между запусками ---

        [Fact]
        public void ParsingSameSourceTwice_ProducesSameStructureAndContent()
        {
            // Стабильность здесь понимается так: количество карточек/квизов/
            // статей, их заголовки и markdown-содержимое должны быть идентичны
            // при повторном парсинге одного и того же файла. Id намеренно
            // исключены из сравнения — см. CardBuilderTests.Id_IsStable_...,
            // который отдельно документирует, что id НЕ стабилен (Guid.NewGuid()).
            var parser = new DeckSourceParser();

            var first = parser.Parse(TestFixtures.FullSampleDeckSource);
            var second = parser.Parse(TestFixtures.FullSampleDeckSource);

            Assert.Equal(first.CategoryHint, second.CategoryHint);

            Assert.Equal(first.Cards.Count, second.Cards.Count);
            for (int i = 0; i < first.Cards.Count; i++)
            {
                var a = first.Cards[i];
                var b = second.Cards[i];
                Assert.Equal(a.Title, b.Title);
                Assert.Equal(a.FrontMarkdown, b.FrontMarkdown);
                Assert.Equal(a.BriefMarkdown, b.BriefMarkdown);
                Assert.Equal(a.DetailedMarkdown, b.DetailedMarkdown);
            }

            Assert.Equal(first.Quizzes[0].Title, second.Quizzes[0].Title);
            Assert.Equal(
                first.Quizzes[0].Questions.Select(q => q.QuestionMarkdown),
                second.Quizzes[0].Questions.Select(q => q.QuestionMarkdown));

            Assert.Equal(first.Articles[0].Title, second.Articles[0].Title);
            Assert.Equal(first.Articles[0].BodyMarkdown, second.Articles[0].BodyMarkdown);
        }

        [Fact]
        public void ParsingIsOrderIndependent_BetweenIndependentDocuments()
        {
            // Парсинг одного документа не должен зависеть от того, что
            // парсилось до него в том же процессе (нет скрытого static state,
            // утечки между вызовами и т.п. — актуально, т.к. ContentMarkdownPipeline
            // создаёт новый MarkdownPipelineBuilder на каждый Build(),
            // но стоит держать это под тестом на будущее).
            var parser = new DeckSourceParser();

            _ = parser.Parse(TestFixtures.QuizWithTwoQuestions);
            var result = parser.Parse(TestFixtures.FullSampleDeckSource);

            Assert.Equal("Топология 🔢", result.CategoryHint);
            Assert.Equal(2, result.Cards.Count);
        }
    }
}
