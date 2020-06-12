using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.CCodeGen;
using LibDerailer.CCodeGen.Statements.Expressions;
using LibDerailer.IR.Expressions;

namespace LibDerailer.IR.Types
{
    public class IRStruct : IRType
    {
        public class IRStructField
        {
            public IRType Type      { get; }
            public string Name      { get; }
            public uint   Offset    { get; }
            public uint   BitOffset { get; }
            public uint   BitCount  { get; }

            public IRStructField(IRType type, string name, uint offset, uint bitOffset = 0, uint bitCount = 0)
            {
                Type      = type;
                Name      = name;
                Offset    = offset;
                BitOffset = bitOffset;
                BitCount  = bitCount;
            }
        }

        public string Name { get; }

        public List<IRStructField> Fields { get; } = new List<IRStructField>();

        public IRStruct(string name, uint size, params IRStructField[] fields)
            : base(size)
        {
            Name = name;
            Fields.AddRange(fields);
        }

        public override CType ToCType()
            => new CType(Name);

        public override bool IsCompatibleWith(IRType b)
            => this == b || (b is IRTypedef td && IsCompatibleWith(td.BaseType));

        public override bool Equals(object obj)
            => ReferenceEquals(obj, this) ||
               (obj is IRMatchType m && m.MatchFunc(this));
    }
}