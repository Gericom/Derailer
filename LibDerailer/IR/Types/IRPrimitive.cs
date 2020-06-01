using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.CCodeGen;
using LibDerailer.IR.Expressions;

namespace LibDerailer.IR.Types
{
    public class IRPrimitive : IRType
    {
        public string Name     { get; }
        public bool   IsSigned   { get; }
        public uint   BitCount { get; }

        private IRPrimitive(string name, bool isSigned, uint bitCount)
        {
            Name     = name;
            IsSigned   = isSigned;
            BitCount = bitCount;
        }

        public override CType ToCType()
            => new CType(Name);

        public IRPrimitive ToSigned()
        {
            if (IsSigned)
                return this;
            if(BitCount < 8)
                throw new IRTypeException();
            switch (BitCount)
            {
                case 8:
                    return S8;
                case 16:
                    return S16;
                case 32:
                    return S32;
                case 64:
                    return S64;
            }
            throw new IRTypeException();
        }

        public IRPrimitive ToUnsigned()
        {
            if (!IsSigned)
                return this;
            if (BitCount < 8)
                throw new IRTypeException();
            switch (BitCount)
            {
                case 8:
                    return U8;
                case 16:
                    return U16;
                case 32:
                    return U32;
                case 64:
                    return U64;
            }
            throw new IRTypeException();
        }

        public override bool Equals(object obj)
            => obj is IRPrimitive p &&
               p.Name == Name &&
               p.IsSigned == IsSigned &&
               p.BitCount == BitCount;

        public static readonly IRPrimitive Void = new IRPrimitive("void", false, 0);
        public static readonly IRPrimitive Bool = new IRPrimitive("BOOL", false, 1);
        public static readonly IRPrimitive S8   = new IRPrimitive("s8", true, 8);
        public static readonly IRPrimitive U8   = new IRPrimitive("u8", false, 8);
        public static readonly IRPrimitive S16  = new IRPrimitive("s16", true, 16);
        public static readonly IRPrimitive U16  = new IRPrimitive("u16", false, 16);
        public static readonly IRPrimitive S32  = new IRPrimitive("s32", true, 32);
        public static readonly IRPrimitive U32  = new IRPrimitive("u32", false, 32);
        public static readonly IRPrimitive S64  = new IRPrimitive("s64", true, 64);
        public static readonly IRPrimitive U64  = new IRPrimitive("u64", false, 64);
    }
}