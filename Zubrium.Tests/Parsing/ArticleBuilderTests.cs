using System;
using System.Collections.Generic;
using System.Text;
using Zubrium.Content.Parsing;
using static Zubrium.Tests.Parsing.ParsingTestHelpers;

namespace Zubrium.Tests.Parsing
{
    public class ArticleBuilderTests
    {
        [Fact]
        public void UsesTitleFromAttribute_EvenIfDifferentH1Present()
        {
            var container = GetSingleContainer(TestFixtures.ArticleWithTitleAttribute, "article");
            var article = ArticleBuilder.FromContainer(container, TestFixtures.ArticleWithTitleAttribute);

            Assert.Equal("Заголовок из атрибута", article.Title);
        }

        [Fact]
        public void FallsBackToFirstH1_WhenTitleAttributeMissing()
        {
            var container = GetSingleContainer(TestFixtures.ArticleWithH1Fallback, "article");
            var article = ArticleBuilder.FromContainer(container, TestFixtures.ArticleWithH1Fallback);

            Assert.Equal("Заголовок статьи", article.Title);
        }

        [Fact]
        public void BodyMarkdown_ContainsFullContent_IncludingFormulas()
        {
            var container = GetSingleContainer(TestFixtures.ArticleWithH1Fallback, "article");
            var article = ArticleBuilder.FromContainer(container, TestFixtures.ArticleWithH1Fallback);

            Assert.Contains("$\\pi_1(S^1)", article.BodyMarkdown);
        }

        [Fact]
        public void GeneratesNonEmptyId_WhenIdAttributeMissing()
        {
            var container = GetSingleContainer(TestFixtures.ArticleWithTitleAttribute, "article");
            var article = ArticleBuilder.FromContainer(container, TestFixtures.ArticleWithTitleAttribute);

            Assert.False(string.IsNullOrWhiteSpace(article.Id));
        }
    }
}
