using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDerailer.CodeGraph.Nodes.IR
{
    public class IRIdentifier : IRExpression
    {
        public IRType Type { get; set; }
        public string Name { get; set; }
    }
}