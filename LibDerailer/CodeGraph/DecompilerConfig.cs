using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDerailer.CodeGraph
{
    public class DecompilerConfig
    {
        public Compiler.Compiler Compiler { get; set; }

        public List<string> Pragmas { get; } = new List<string>();

        public List<string> Includes { get; } = new List<string>();
    }
}
