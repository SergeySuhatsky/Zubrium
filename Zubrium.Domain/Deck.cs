using System;
using System.Collections.Generic;
using System.Text;

namespace Zubrium.Domain
{
    public class Deck
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public List<Card> Cards { get; set; }
    }
}
