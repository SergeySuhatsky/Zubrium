using System;
using System.Collections.Generic;
using System.Text;

namespace Zubrium.Domain
{
    public class QuizQuestion
    {
        public QuizQuestion(string questionMarkdown, List<AnswerOption> options, string? explanationMarkdown)
        {
            QuestionMarkdown = questionMarkdown;
            Options = options;
            ExplanationMarkdown = explanationMarkdown;
        }

        public string QuestionMarkdown { get; set; }

        public List<AnswerOption> Options { get; set; } = new List<AnswerOption>();

        public string? ExplanationMarkdown { get; set; }
    }
}
