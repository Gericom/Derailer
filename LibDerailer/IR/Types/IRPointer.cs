﻿using System;
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
            : base(4)
        {
            BaseType = baseType;
        }

        public override CType ToCType()
            => new CType(BaseType.ToCType(), true);

        public override bool IsCompatibleWith(IRType b)
            => b is IRPointer || b == IRPrimitive.U32 || (b is IRTypedef td && IsCompatibleWith(td.BaseType));

        public override bool Equals(object obj)
            => (obj is IRPointer p && p.BaseType.Equals(BaseType)) ||
               (obj is IRMatchType m && m.MatchFunc(this));
    }
}