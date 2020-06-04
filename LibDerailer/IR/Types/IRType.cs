using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.CCodeGen;
using LibDerailer.IO.Elf.Dwarf2;

namespace LibDerailer.IR.Types
{
    public abstract class IRType
    {
        public IRPointer Pointer => new IRPointer(this);

        public abstract CType ToCType();

        public static bool operator ==(IRType a, IRType b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(IRType a, IRType b)
        {
            return !a.Equals(b);
        }

        // public static IRType FromDwarf(Dwarf2Die die)
        // {
        //     if (!(die is Dwarf2ClassType) &&
        //         !(die is Dwarf2Typedef) &&
        //         !(die is Dwarf2StructureType) &&
        //         !(die is Dwarf2UnionType) &&
        //         !(die is Dwarf2BaseType))
        //         return null;
        //
        //     while (die is Dwarf2Typedef tdef)
        //         die = tdef.Type;
        //
        //     return null;
        // }
    }
}