using Markdig;
using Markdig.Extensions.CustomContainers;
using Markdig.Syntax;
using System;
using System.Collections.Generic;
using System.Text;
using Zubrium.Content.Parsing;

namespace Zubrium.Tests.Parsing
{
    internal static class ParsingTestHelpers
    {
        public static MarkdownDocument ParseDocument(string markdown)
        {
            var pipeline = ContentMarkdownPipeline.Build();
            return Markdown.Parse(markdown, pipeline);
        }

        public static List<CustomContainer> GetContainers(string markdown, string info)
        {
            var document = ParseDocument(markdown);
            return document.Descendants<CustomContainer>()
                .Where(c => c.Info == info)
                .ToList();
        }

        public static CustomContainer GetSingleContainer(string markdown, string info)
        {
            var containers = GetContainers(markdown, info);
            return containers.Single();
        }
    }
}
