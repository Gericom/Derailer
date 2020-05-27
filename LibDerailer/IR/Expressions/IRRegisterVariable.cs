using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDerailer.IR.Expressions
{
    public class IRRegisterVariable : IRVariable
    {
        public IRRegisterVariable(IRType type, string name) 
            : base(type, name)
        {

        }
    }
}
