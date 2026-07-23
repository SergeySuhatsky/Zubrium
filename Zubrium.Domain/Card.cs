using System;
using System.Collections.Generic;
using System.Text;

namespace Zubrium.Domain
{
    public class Card
    {
        public Card(string id, string frontMarkdown, string briefMarkdown, string? detailedMarkdown = null, string? title = null)
        {
            Id = id;
            Title = title;
            FrontMarkdown = frontMarkdown;
            BriefMarkdown = briefMarkdown;
            DetailedMarkdown = detailedMarkdown;
        }

        public string Id { get; set; }

        public string? Title { get; set; }

        public string FrontMarkdown { get; set; }

        public string BriefMarkdown { get; set; }

        public string? DetailedMarkdown { get; set; }

    }
}
