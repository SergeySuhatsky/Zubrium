using System;
using System.Collections.Generic;
using System.Text;

namespace Zubrium.Domain
{
    public class ParsedContentSet
    {
        public string? CategoryHint { get; set; }
        public List<Article> Articles { get; set; } = new List<Article>();
        public List<QuizBlock> Quizzes { get; set; } = new List<QuizBlock>();
        public List<Card> Cards { get; set; } = new List<Card>();
        public List<string> Warnings { get; set; } = new List<string>();
    }
}
