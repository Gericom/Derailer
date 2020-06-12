using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.CCodeGen;
using LibDerailer.IO.Elf.Dwarf2;
using LibDerailer.IO.Elf.Dwarf2.Enums;

namespace LibDerailer.IR.Types
{
    public abstract class IRType
    {
        public uint ByteSize { get; }

        protected IRType(uint byteSize)
        {
            ByteSize = byteSize;
        }

        public IRPointer GetPointer() => new IRPointer(this);

        public virtual IRType GetRootType() => this;

        public abstract CType ToCType();

        public static bool operator ==(IRType a, IRType b)
            => a.Equals(b);

        public static bool operator !=(IRType a, IRType b)
            => !a.Equals(b);

        public abstract bool IsCompatibleWith(IRType b);

        public static Dictionary<Dwarf2Die, IRType> FromDwarf(Dwarf2CompileUnit compileUnit)
        {
            var dict = new Dictionary<Dwarf2Die, IRType>();
            foreach (var die in compileUnit.Children)
            {
                switch (die)
                {
                    case Dwarf2BaseType bt:
                        if (bt.ByteSize == 4 && bt.Encoding == Dwarf2Encoding.Signed)
                            dict.Add(bt, IRPrimitive.S32);
                        else if (bt.ByteSize == 4 && bt.Encoding == Dwarf2Encoding.Unsigned)
                            dict.Add(bt, IRPrimitive.U32);
                        else if (bt.ByteSize == 2 && bt.Encoding == Dwarf2Encoding.Signed)
                            dict.Add(bt, IRPrimitive.S16);
                        else if (bt.ByteSize == 2 && bt.Encoding == Dwarf2Encoding.Unsigned)
                            dict.Add(bt, IRPrimitive.U16);
                        else if (bt.ByteSize == 1 && bt.Encoding == Dwarf2Encoding.SignedChar)
                            dict.Add(bt, IRPrimitive.S8);
                        else if (bt.ByteSize == 1 && bt.Encoding == Dwarf2Encoding.UnsignedChar)
                            dict.Add(bt, IRPrimitive.U8);
                        else if(bt.ByteSize == 0)
                            dict.Add(bt, IRPrimitive.Void);
                        break;
                }
            }

            foreach (var die in compileUnit.Children)
            {
                switch (die)
                {
                    case Dwarf2ClassType ct:
                        dict.Add(ct, new IRStruct(ct.Name ?? "", ct.ByteSize));
                        break;
                    case Dwarf2StructureType st:
                        dict.Add(st, new IRStruct(st.Name ?? "", st.ByteSize));
                        break;
                }
            }

            foreach (var die in compileUnit.Children)
            {
                switch (die)
                {
                    case Dwarf2Typedef td:
                        if(dict.ContainsKey(td.Type))
                            dict.Add(td, new IRTypedef(dict[td.Type], td.Name ?? ""));
                        break;
                }
            }

            IRType getType(Dwarf2Die type)
            {
                IRType irType;
                if (type is Dwarf2PointerType ptr)
                    irType = getType(ptr.Type)?.GetPointer();
                else if (type is Dwarf2ArrayType arr)
                {
                    var elementType = getType(arr.Type);
                    irType = new IRArray(elementType, arr.ByteSize / elementType.ByteSize);
                }
                else if (type is Dwarf2SubroutineType sub)
                {
                    //not supported yet
                    irType = IRPrimitive.Void.GetPointer();
                }
                else if(type is Dwarf2UnionType)
                {
                    //not supported yet
                    return null;
                }
                else
                {
                    if (!dict.ContainsKey(type))
                        return null;
                    irType = dict[type];
                }

                return irType;
            }

            //todo: support class inheritance
            foreach (var pair in dict)
            {
                if (!(pair.Key is Dwarf2ClassType || pair.Key is Dwarf2StructureType))
                    continue;
                var irStruct = (IRStruct) pair.Value;
                foreach (var member in pair.Key.Children)
                {
                    if (!(member is Dwarf2Member m))
                        continue;
                    var type = getType(m.Type);
                    if (type is null)
                        continue;
                    irStruct.Fields.Add(new IRStruct.IRStructField(
                        type, m.Name, m.DataMemberLocation, m.BitOffset == 0 ? 0 : (32 - m.BitOffset - m.BitSize), m.BitSize));
                }
            }

            return dict;
        }
    }
}