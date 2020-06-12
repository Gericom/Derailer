using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.CCodeGen;

namespace LibDerailer.IR.Types
{
    public class IRTypedef : IRType
    {
        public string Name     { get; }
        public IRType BaseType { get; }

        public IRTypedef(IRType baseType, string name)
            : base(baseType.ByteSize)
        {
            Name     = name;
            BaseType = baseType;
        }

        public override IRType GetRootType()
            => BaseType.GetRootType();

        public override CType ToCType()
            => new CType(Name);

        public override bool IsCompatibleWith(IRType b)
            => BaseType.IsCompatibleWith(b);

        public override bool Equals(object obj)
            => ReferenceEquals(this, obj) ||
               (obj is IRMatchType m && m.MatchFunc(this));
    }
}