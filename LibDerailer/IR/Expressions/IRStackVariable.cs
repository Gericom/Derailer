using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.IR.Types;

namespace LibDerailer.IR.Expressions
{
    public class IRStackVariable : IRVariable
    {
        public IRStackVariable(IRType type, string name) 
            : base(type, name)
        {
        }
    }
}
