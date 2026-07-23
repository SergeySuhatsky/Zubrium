using System;
using System.Collections.Generic;
using System.Text;

namespace Zubrium.Domain
{
    public class Article
    {
        public Article(string id, string title, string bodyMarkdown)
        {
            Id = id;
            Title = title;
            BodyMarkdown = bodyMarkdown;
        }

        public string Id { get; set; }

        public string Title { get; set; }
        
        public string BodyMarkdown { get; set; }


    }
}
