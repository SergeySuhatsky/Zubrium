using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Zubrium.Content.Parsing;
using static Zubrium.Tests.Parsing.ParsingTestHelpers;

namespace Zubrium.Tests.Parsing
{
    public class CardBuilderTests
    {
        [Fact]
        public void UsesTitleFromAttribute_WhenPresent()
        {
            var container = GetSingleContainer(TestFixtures.CardWithTitleAttribute, "card");
            var card = CardBuilder.FromContainer(container, TestFixtures.CardWithTitleAttribute);

            Assert.Equal("Открытое множество", card.Title);
        }

        [Fact]
        public void FallsBackToFirstH1_WhenTitleAttributeMissing()
        {
            var container = GetSingleContainer(TestFixtures.CardWithH1Fallback, "card");
            var card = CardBuilder.FromContainer(container, TestFixtures.CardWithH1Fallback);

            Assert.Equal("Гомеоморфизм", card.Title);
        }

        [Fact]
        public void FallsBackToDefaultTitle_WhenNoAttributeAndNoH1()
        {
            var container = GetSingleContainer(TestFixtures.CardWithoutTitleAndWithoutH1, "card");
            var card = CardBuilder.FromContainer(container, TestFixtures.CardWithoutTitleAndWithoutH1);

            Assert.Equal("Без названия", card.Title);
        }

        [Fact]
        public void SplitsFrontBriefDetailed_Correctly()
        {
            var container = GetSingleContainer(TestFixtures.CardWithTitleAttribute, "card");
            var card = CardBuilder.FromContainer(container, TestFixtures.CardWithTitleAttribute);

            Assert.Contains("Что называют открытым множеством?", card.FrontMarkdown);
            Assert.DoesNotContain("brief", card.FrontMarkdown);

            Assert.Equal("Любое множество из топологии.", card.BriefMarkdown.Trim());

            Assert.NotNull(card.DetailedMarkdown);
            Assert.Equal("Развёрнутое определение через топологию τ.", card.DetailedMarkdown!.Trim());
        }

        [Fact]
        public void DetailedMarkdown_IsNull_WhenSectionAbsent()
        {
            var container = GetSingleContainer(TestFixtures.CardWithoutDetailed, "card");
            var card = CardBuilder.FromContainer(container, TestFixtures.CardWithoutDetailed);

            Assert.Null(card.DetailedMarkdown);
            Assert.False(string.IsNullOrWhiteSpace(card.BriefMarkdown));
        }

        [Fact]
        public void BriefMarkdown_IsEmpty_WhenSectionAbsentButDetailedPresent()
        {
            // Важный краевой случай: если "--- brief" отсутствует, а "--- detailed"
            // есть, весь текст до "--- detailed" уходит в frontMarkdown,
            // а briefMarkdown остаётся пустой строкой. Если фронтенд ожидает,
            // что brief всегда непустой (например, для превью карточки),
            // это стоит явно обработать в CardBuilder — тест фиксирует
            // текущее поведение.
            var container = GetSingleContainer(TestFixtures.CardWithoutBrief, "card");
            var card = CardBuilder.FromContainer(container, TestFixtures.CardWithoutBrief);

            Assert.Equal(string.Empty, card.BriefMarkdown);
            Assert.NotNull(card.DetailedMarkdown);
        }

        [Fact]
        public void GeneratesNonEmptyId_WhenIdAttributeMissing()
        {
            var container = GetSingleContainer(TestFixtures.CardWithTitleAttribute, "card");
            var card = CardBuilder.FromContainer(container, TestFixtures.CardWithTitleAttribute);

            Assert.False(string.IsNullOrWhiteSpace(card.Id));
        }

        [Fact]
        public void UsesExplicitId_WhenIdAttributeProvided()
        {
            const string markdown = """
                ::: card {#my-stable-id title="С явным id"}
                Вопрос.

                --- brief
                Ответ.
                :::
                """;

            var container = GetSingleContainer(markdown, "card");
            var card = CardBuilder.FromContainer(container, markdown);

            Assert.Equal("my-stable-id", card.Id);
        }

        // --- Тест на СТАБИЛЬНОСТЬ id между повторными парсингами ---

        [Fact]
        public void Id_IsStable_AcrossRepeatedParsesOfSameSource()
        {
            // Если у карточки нет {#id}, CardBuilder генерирует случайный
            // Guid.NewGuid(). Это значит, что повторный парсинг ОДНОГО И ТОГО ЖЕ
            // markdown-файла (например, при переимпорте контента) даёт РАЗНЫЕ id
            // для одних и тех же карточек. Для контент-пайплайна, где id, скорее
            // всего, используется как внешний ключ (прогресс пользователя,
            // spaced-repetition и т.п.), это серьёзный источник нестабильности.
            //
            // Тест ниже документирует текущее (нежелательное) поведение.
            // Если в CardBuilder добавят детерминированную генерацию id
            // (например, хэш от title+frontMarkdown), этот тест нужно
            // инвертировать на Assert.Equal.
            var container1 = GetSingleContainer(TestFixtures.CardWithTitleAttribute, "card");
            var card1 = CardBuilder.FromContainer(container1, TestFixtures.CardWithTitleAttribute);

            var container2 = GetSingleContainer(TestFixtures.CardWithTitleAttribute, "card");
            var card2 = CardBuilder.FromContainer(container2, TestFixtures.CardWithTitleAttribute);

            Assert.NotEqual(card1.Id, card2.Id); // текущее поведение: id "плавает"
        }

        // --- Тест на устойчивость к CRLF (Windows line endings) ---

        [Fact]
        public void ParsesIdenticalContent_RegardlessOfLineEndingStyle()
        {
            // CardBuilder.ExtractInnerMarkdown режет текст только по '\n',
            // в то время как QuizBuilder и ArticleBuilder режут и по "\r\n",
            // и по "\n". На файле, сохранённом с CRLF (типично для Windows/Git
            // с autocrlf=true), это может оставлять хвостовые '\r' в
            // содержимом карточки, которых нет у статьи/квиза из того же файла.
            var lfContainer = GetSingleContainer(TestFixtures.CardWithTitleAttribute, "card");
            var lfCard = CardBuilder.FromContainer(lfContainer, TestFixtures.CardWithTitleAttribute);

            var crlfContainer = GetSingleContainer(TestFixtures.CardWithCrlfLineEndings, "card");
            var crlfCard = CardBuilder.FromContainer(crlfContainer, TestFixtures.CardWithCrlfLineEndings);

            // Ожидание: контент карточки не должен зависеть от стиля переводов строк
            // в исходном файле. Если этот тест падает — значит остаются
            // символы '\r' внутри front/brief/detailed при CRLF-исходнике.
            Assert.Equal(lfCard.FrontMarkdown, crlfCard.FrontMarkdown.Replace("\r", ""));
            Assert.Equal(lfCard.BriefMarkdown, crlfCard.BriefMarkdown.Replace("\r", ""));
            Assert.DoesNotContain("\r", crlfCard.FrontMarkdown);
            Assert.DoesNotContain("\r", crlfCard.BriefMarkdown);
        }
    }
}
