using System;
using System.Collections.Generic;
using System.Text;

namespace Zubrium.Domain
{
    public class Article
    {
        public Article(string id, string bodyMarkdown, string title = "Заголовок статьи", string? categoryId = null)
        {
            Id = id;
            Title = title;
            BodyMarkdown = bodyMarkdown;
            CategoryId = categoryId;
        }

        public string Id { get; set; }

        public string Title { get; set; }
        
        public string BodyMarkdown { get; set; }

        public string CategoryId { get; set; }


    }
}
