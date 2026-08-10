using SQLite;
using System;

namespace Zubrium.Persistence.Entities
{
    [Table("DailyActivities")]
    public class DailyActivityEntity
    {
        [PrimaryKey]
        public DateTime Date { get; set; } // Сохраняем только дату (DateOnly)
        public int NewCardsStudied { get; set; }
        public int ReviewCardsStudied { get; set; }
    }
}
