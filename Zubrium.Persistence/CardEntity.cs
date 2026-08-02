using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Zubrium.Persistence
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
        public string CategoryId { get; set; }


    }
}
