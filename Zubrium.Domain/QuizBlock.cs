using System;
using System.Collections.Generic;
using System.Text;

namespace Zubrium.Domain
{
    public class QuizBlock
    {
        public QuizBlock(string? title, List<QuizQuestion> questions)
        {
            Title = title;
            Questions = questions;
        }

        public string? Title { get; set; }
        public List<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
    }
}
