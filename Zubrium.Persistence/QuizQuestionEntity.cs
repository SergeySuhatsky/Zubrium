using SQLite;
using System;
using System.Collections.Generic;
using System.Text;
using Zubrium.Domain;

namespace Zubrium.Persistence
{
    public class QuizQuestionEntity
    {
        [PrimaryKey]
        public string Id { get; set; }

        public string Title { get; set; }

        [Indexed]
        public string CategoryId { get; set; }

        // Сюда мы будем сохранять List<QuizQuestion> в виде JSON-строки
        public string QuestionsJson { get; set; }
    }
}
