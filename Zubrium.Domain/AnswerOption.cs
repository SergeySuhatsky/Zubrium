using System;
using System.Collections.Generic;
using System.Text;

namespace Zubrium.Domain
{
    public class AnswerOption
    {
        public AnswerOption(string textMarkdown, bool isCorrect)
        {
            TextMarkdown = textMarkdown;
            IsCorrect = isCorrect;
        }

        public string TextMarkdown { get; set; }
        public bool IsCorrect { get; set; }
    }
}
