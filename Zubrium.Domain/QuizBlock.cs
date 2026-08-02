using System;
using System.Collections.Generic;
using System.Text;

namespace Zubrium.Domain
{
    public class QuizBlock
    {
        public QuizBlock(string id, List<QuizQuestion> questions, string title = "Заголовок статьи", string? categoryId=null)
        {
            Id=id;
            Title = title;
            Questions = questions;
            CategoryId = categoryId;
        }

        public string Id { get; set; }

        public string Title { get; set; }
        public List<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();

        public string? CategoryId { get; set; }
    }
}
