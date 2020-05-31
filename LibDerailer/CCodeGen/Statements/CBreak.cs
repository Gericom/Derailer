using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDerailer.CCodeGen.Statements
{
    public class CBreak : CStatement
    {
        public override string ToString() => "break;";
    }
}
