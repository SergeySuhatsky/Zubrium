using SQLite;

namespace Zubrium.Persistence
{
    [Table("Articles")]
    public class ArticleEntity
    {
        [PrimaryKey]
        public string Id { get; set; }

        public string Title { get; set; }

        public string BodyMarkdown { get; set; }
    }
}
