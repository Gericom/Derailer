using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.CCodeGen;

namespace LibDerailer.IR.Types
{
    public class IRStruct : IRType
    {
        public struct IRStructField
        {
            public IRType Type      { get; }
            public string Name      { get; }
            public uint   Offset    { get; }
            public uint   BitOffset { get; }
            public uint   BitCount  { get; }
        }

        public string Name { get; }

        public IRStructField[] Fields { get; }

        public override CType ToCType()
            => new CType(Name);
    }
}