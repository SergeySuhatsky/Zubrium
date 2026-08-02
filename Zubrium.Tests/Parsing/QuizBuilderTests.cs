using System;
using System.Collections.Generic;
using System.Text;
using Zubrium.Content.Parsing;
using static Zubrium.Tests.Parsing.ParsingTestHelpers;

namespace Zubrium.Tests.Parsing
{
    public class QuizBuilderTests
    {
        [Fact]
        public void UsesTitleFromAttribute_WhenPresent()
        {
            var container = GetSingleContainer(TestFixtures.QuizWithTwoQuestions, "quiz");
            var quiz = QuizBuilder.FromContainer(container, TestFixtures.QuizWithTwoQuestions);

            Assert.Equal("Проверка знаний", quiz.Title);
        }

        [Fact]
        public void SplitsIntoCorrectNumberOfQuestions()
        {
            var container = GetSingleContainer(TestFixtures.QuizWithTwoQuestions, "quiz");
            var quiz = QuizBuilder.FromContainer(container, TestFixtures.QuizWithTwoQuestions);

            Assert.Equal(2, quiz.Questions.Count);
        }

        [Fact]
        public void ParsesOptionsAndMarksCorrectAnswer_CaseInsensitive()
        {
            var container = GetSingleContainer(TestFixtures.QuizWithTwoQuestions, "quiz");
            var quiz = QuizBuilder.FromContainer(container, TestFixtures.QuizWithTwoQuestions);

            var firstQuestion = quiz.Questions[0];
            Assert.Equal(2, firstQuestion.Options.Count);
            Assert.True(firstQuestion.Options[0].IsCorrect);
            Assert.False(firstQuestion.Options[1].IsCorrect);
            Assert.Equal("Верно", firstQuestion.Options[0].TextMarkdown);
            Assert.Equal("Неверно", firstQuestion.Options[1].TextMarkdown);
        }

        [Fact]
        public void ExtractsExplanation_WhenPresent()
        {
            var container = GetSingleContainer(TestFixtures.QuizWithTwoQuestions, "quiz");
            var quiz = QuizBuilder.FromContainer(container, TestFixtures.QuizWithTwoQuestions);

            var firstQuestion = quiz.Questions[0];
            Assert.NotNull(firstQuestion.ExplanationMarkdown);
            Assert.Equal("Пояснение к первому вопросу.", firstQuestion.ExplanationMarkdown!.Trim());
        }

        [Fact]
        public void Explanation_IsNull_WhenSectionAbsent()
        {
            var container = GetSingleContainer(TestFixtures.QuizWithTwoQuestions, "quiz");
            var quiz = QuizBuilder.FromContainer(container, TestFixtures.QuizWithTwoQuestions);

            var secondQuestion = quiz.Questions[1];
            Assert.Null(secondQuestion.ExplanationMarkdown);
        }

        [Fact]
        public void QuestionMarkdown_ExcludesOptionLines()
        {
            var container = GetSingleContainer(TestFixtures.QuizWithTwoQuestions, "quiz");
            var quiz = QuizBuilder.FromContainer(container, TestFixtures.QuizWithTwoQuestions);

            var secondQuestion = quiz.Questions[1];
            Assert.Contains("Второй вопрос?", secondQuestion.QuestionMarkdown);
            Assert.DoesNotContain("Вариант A", secondQuestion.QuestionMarkdown);
        }

        [Fact]
        public void ParsesThreeOptions_WhenThreeProvided()
        {
            var container = GetSingleContainer(TestFixtures.QuizWithTwoQuestions, "quiz");
            var quiz = QuizBuilder.FromContainer(container, TestFixtures.QuizWithTwoQuestions);

            var secondQuestion = quiz.Questions[1];
            Assert.Equal(3, secondQuestion.Options.Count);
            Assert.True(secondQuestion.Options[1].IsCorrect); // "Вариант B"
        }

        // --- Regression-тест на найденный баг ---

        [Fact]
        public void TitleFallback_ShouldUseContainerLocalText_NotWholeDocument()
        {
            // БАГ в текущем QuizBuilder.FromContainer:
            // когда title-атрибут отсутствует, код ищет первый "## " не в
            // тексте ТЕКУЩЕГО контейнера (как это корректно сделано в
            // CardBuilder и ArticleBuilder через frontMarkdown/bodyMarkdown),
            // а во всём rawSource целиком:
            //
            //     var firstH1 = rawSource
            //         .Split(...)
            //         .FirstOrDefault(line => line.StartsWith("## "));
            //
            // В файле с несколькими ::: quiz-блоками это значит, что ВТОРОЙ
            // квиз без {title=...} получит заголовок ПЕРВОГО H2 во всём
            // документе — то есть заголовок первого квиза, а не своего
            // собственного вопроса.
            //
            // Этот тест написан так, как должно работать по смыслу кода
            // (заголовок берётся из своего блока). На текущей реализации
            // он ПАДАЕТ и тем самым фиксирует баг для трекера задач.
            var container = GetContainers(
                TestFixtures.TwoQuizzesSecondWithoutTitleAttribute, "quiz")[1];

            var quiz = QuizBuilder.FromContainer(
                container, TestFixtures.TwoQuizzesSecondWithoutTitleAttribute);

            Assert.Equal("Вопрос из второго квиза?", quiz.Title);
        }
    }
}
