using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.CCodeGen;

namespace LibDerailer.IR.Types
{
    public class IRMatchType : IRType
    {
        public delegate bool OnMatch(IRType type);

        public OnMatch MatchFunc { get; }

        public IRMatchType(OnMatch matchFunc)
            : base(0)
        {
            MatchFunc = matchFunc ?? throw new ArgumentNullException(nameof(matchFunc));
        }

        public override CType ToCType()
            => throw new InvalidOperationException("IRMatchType is only to be used for pattern matching!");

        public override bool IsCompatibleWith(IRType b)
            => this == b || (b is IRTypedef t && IsCompatibleWith(t));

        public override bool Equals(object obj)
            => obj is IRType t && MatchFunc(t);
    }
}