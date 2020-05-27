using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.CCodeGen;

namespace LibDerailer.IR.Expressions
{
    public static class IRTypeUtil
    {
        public static CType ToCType(this IRType type, bool signed)
        {
            switch (type)
            {
                case IRType.Void:
                    return new CType("void");
                case IRType.I1:
                    return new CType("BOOL");
                case IRType.I8:
                    return new CType(signed ? "s8" : "u8");
                case IRType.I16:
                    return new CType(signed ? "s16" : "u16");
                case IRType.I32:
                    return new CType(signed ? "s32" : "u32");
                case IRType.I64:
                    return new CType(signed ? "s64" : "u64");
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}
