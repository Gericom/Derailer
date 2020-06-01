using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.CCodeGen;

namespace LibDerailer.IR.Types
{
    public abstract class IRType
    {
        public IRPointer Pointer => new IRPointer(this);

        public abstract CType ToCType();

        public static bool operator ==(IRType a, IRType b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(IRType a, IRType b)
        {
            return !a.Equals(b);
        }
    }
}
