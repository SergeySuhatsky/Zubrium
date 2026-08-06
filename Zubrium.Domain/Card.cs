using System;

namespace Zubrium.Domain
{
    public class Card
    {
        public Card(string id, string frontMarkdown, string briefMarkdown, string title = "Заголовок карточки", string? detailedMarkdown = null, string? categoryId = null)
        {
            Id = id;
            Title = title;
            FrontMarkdown = frontMarkdown;
            BriefMarkdown = briefMarkdown;
            DetailedMarkdown = detailedMarkdown;
            CategoryId = categoryId;

            State = 1; 
            Step = 0;
            Due = DateTime.UtcNow;
            IsKnown = false;
            IsMastered = false;
        }

        public string Id { get; set; }
        public string Title { get; set; }
        public string FrontMarkdown { get; set; }
        public string BriefMarkdown { get; set; }
        public string? DetailedMarkdown { get; set; }
        public string? CategoryId { get; set; }

        public bool IsKnown { get; set; }
        public bool IsMastered { get; set; }

        public int State { get; set; }
        public int? Step { get; set; }
        public DateTime Due { get; set; }
        public double? Stability { get; set; }
        public double? Difficulty { get; set; }

        public int ElapsedDays { get; set; }
        public int ScheduledDays { get; set; }
        public int Reps { get; set; }
        public int Lapses { get; set; }
        public DateTime? LastReview { get; set; }

        public string NextReviewText 
        {
            get 
            {
                if (IsKnown) return "Уже изучено";
                if (IsMastered) return "Выучено";
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
