using Markdig;
using System;
using System.Collections.Generic;
using System.Text;

namespace Zubrium.Content.Parsing
{
    public static class ContentMarkdownPipeline
    {
        public static MarkdownPipeline Build()
        {
            return new MarkdownPipelineBuilder()
                .UseCustomContainers()   // Поддержка ::: 
                .UseGenericAttributes()  // Поддержка {#id .class}
                .UseYamlFrontMatter()    // Поддержка метаданных в начале файла
                .UsePipeTables()         // Поддержка таблиц в тексте статьи
                .Build();
        }
    }
}
