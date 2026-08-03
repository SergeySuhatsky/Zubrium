using System;
using System.Collections.Generic;
using System.Text;
using Zubrium.Domain;

namespace Zubrium.Content.Parsing
{
    public interface IDeckSourceParser
    {
        ParsedContentSet Parse(string rawMarkdown);
    }
}
