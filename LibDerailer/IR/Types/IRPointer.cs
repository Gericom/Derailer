using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.CCodeGen;

namespace LibDerailer.IR.Types
{
    public class IRPointer : IRType
    {
        public IRType BaseType { get; }

        public IRPointer(IRType baseType)
        {
            BaseType = baseType;
        }

        public override CType ToCType()
            => new CType(BaseType.ToCType(), true);

        public override bool Equals(object obj)
            => obj is IRPointer p && p.BaseType.Equals(BaseType);
    }
}