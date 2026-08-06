using System;

namespace Zubrium.Domain
{
    public class Card
    {
        public Card(string id, string frontMarkdown, string briefMarkdown, string title="Заголовок карточки", string? detailedMarkdown = null, string? categoryId = null)
        {
            Id = id;
            Title = title;
            FrontMarkdown = frontMarkdown;
            BriefMarkdown = briefMarkdown;
            DetailedMarkdown = detailedMarkdown;
            CategoryId = categoryId;

            // Инициализация по умолчанию
            State = 0; 
            Due = DateTime.UtcNow;
            IsKnown = false;
        }

        public string Id { get; set; }
        public string Title { get; set; }
        public string FrontMarkdown { get; set; }
        public string BriefMarkdown { get; set; }
        public string? DetailedMarkdown { get; set; }
        public string? CategoryId { get; set; }

        // === Пользовательские флаги ===
        public bool IsKnown { get; set; } // Отсеивает карточки при первом знакомстве

        // === FSRS ПОЛЯ ===
        public int State { get; set; }
        public DateTime Due { get; set; }
        public double Stability { get; set; }
        public double Difficulty { get; set; }
        public int ElapsedDays { get; set; }
        public int ScheduledDays { get; set; }
        public int Reps { get; set; }
        public int Lapses { get; set; }
        public DateTime? LastReview { get; set; }

        // Вычисляемое свойство для списков (UI)
        public string NextReviewText 
        {
            get 
            {
                if (IsKnown) return "Уже изучено";
                if (Reps == 0) return "Новая";

                var diff = Due.Date - DateTime.UtcNow.Date;
                if (diff.Days < 0) return "Просрочено";
                if (diff.Days == 0) return "Сегодня";
                if (diff.Days == 1) return "Завтра";
                return $"Через {diff.Days} дн.";
            }
        }
    }
}
