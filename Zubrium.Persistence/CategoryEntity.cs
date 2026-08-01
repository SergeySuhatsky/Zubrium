using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Zubrium.Persistence
{
    public class CategoryEntity
    {
        [PrimaryKey]
        public string DbId { get; set; }

        [MaxLength(100)]
        public string Name { get; set; }
    }
}
