using System;
using System.Collections.Generic;
using System.Text;
using Zubrium.Domain;
using Zubrium.Persistence.Entities;

namespace Zubrium.Persistence.Mappers
{
    public static class QuizMapper
    {
        public static QuizBlockEntity ToEntity(this QuizBlock quiz)
        {
            if (quiz == null) return null;

            var jsonOptions = new System.Text.Json.JsonSerializerOptions
            {
                // Разрешаем не экранировать кириллицу и другие символы
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                // (Опционально) делает JSON читаемым, с переносами строк
                WriteIndented = true
            };

            
            return new QuizBlockEntity
            {
                Id = quiz.Id,
                Title = quiz.Title,
                CategoryId = quiz.CategoryId,
                QuestionsJson = System.Text.Json.JsonSerializer.Serialize(quiz.Questions, jsonOptions)
            };
        }

        public static QuizBlock ToDomain (this QuizBlockEntity quizEntity) 
        {

            if (quizEntity == null) return null;

            return new QuizBlock
            (
                id: quizEntity.Id,
                title: quizEntity.Title,
                categoryId: quizEntity.CategoryId,
                questions: System.Text.Json.JsonSerializer.Deserialize<List<QuizQuestion>>(quizEntity.QuestionsJson)

            );
        }
    }
}
