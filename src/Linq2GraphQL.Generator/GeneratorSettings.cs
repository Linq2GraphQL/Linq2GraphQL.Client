using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linq2GraphQL.Generator
{
    public class GeneratorSettings
    {
        public static GeneratorSettings Current { get; set; }

        public bool Nullable { get; set; }
    }

    
}
