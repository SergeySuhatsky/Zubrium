using SQLite;

namespace Zubrium.Persistence.Entities
{
    [Table("Articles")]
    public class ArticleEntity
    {
        [PrimaryKey]
        public string Id { get; set; }

        public string Title { get; set; }

        public string BodyMarkdown { get; set; }

        [Indexed]
        public string? CategoryId { get; set; }
    }
}
