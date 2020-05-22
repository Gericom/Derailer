using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDerailer.CodeGraph
{
    public class Constant : Operand
    {
        public uint Value { get; }

        public Constant(uint value)
        {
            Value = value;
        }
    }
}
