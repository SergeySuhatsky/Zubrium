using SQLite;
using System;

namespace Zubrium.Persistence.Entities
{
    [Table("Cards")]
    public class CardEntity
    {
        [PrimaryKey]
        public string Id { get; set; }
        public string Title { get; set; }
        public string FrontMarkdown { get; set; }
        public string BriefMarkdown { get; set; }
        public string? DetailedMarkdown { get; set; }

        [Indexed]
        public string? CategoryId { get; set; }

        public bool IsKnown { get; set; }
        public bool IsMastered { get; set; }

        [Indexed]
        public DateTime Due { get; set; }
        public int Reps { get; set; }
        public DateTime? LastReview { get; set; }

        // Текстовое поле для сохранения всех кастомных переменных алгоритма
        public string? AlgorithmDataJson { get; set; }
    }
}
