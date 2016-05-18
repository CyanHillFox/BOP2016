using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BOP
{
    class Author
    {
        public Author(Int64 id, HashSet<Int64> papers, HashSet<Int64> affs)
        {
            AuId = id;
            Papers = papers;
            Affiliations = affs;
        }

        public readonly Int64 AuId;

        public readonly HashSet<Int64> Papers;

        public readonly HashSet<Int64> Affiliations;
    }
}
