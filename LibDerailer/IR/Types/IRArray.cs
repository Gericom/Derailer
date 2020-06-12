using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.CCodeGen;

namespace LibDerailer.IR.Types
{
    public class IRArray : IRType
    {
        public IRType ElementType { get; }
        public uint   Length      { get; }

        public IRArray(IRType elementType, uint length)
            : base(elementType.ByteSize * length)
        {
            ElementType = elementType;
            Length      = length;
        }

        public override CType ToCType() => throw new NotImplementedException();

        public override bool IsCompatibleWith(IRType b)
            => this == b || (b is IRTypedef t && IsCompatibleWith(t.BaseType));

        public override bool Equals(object obj)
            => (obj is IRArray a && a.ElementType == ElementType && a.Length == Length) ||
               (obj is IRMatchType m && m.MatchFunc(this));
    }
}