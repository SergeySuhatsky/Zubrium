using System;
using System.Collections.Generic;
using System.Text;

namespace Zubrium.Domain
{
    public class QuizBlock
    {
        public QuizBlock( List<QuizQuestion> questions, string title = "Заголовок статьи", string? categoryId=null)
        {
            Title = title;
            Questions = questions;
            CategoryId = categoryId;
        }

        public string Title { get; set; }
        public List<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();

        public string? CategoryId { get; set; }
    }
}
