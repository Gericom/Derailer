using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.IO.Elf.Dwarf2.Enums;

namespace LibDerailer.IO.Elf.Dwarf2
{
    public class Dwarf2CompileUnit : Dwarf2Die
    {
        public Dwarf2Language Language => (Dwarf2Language) GetAttribute<uint>(Dwarf2Attribute.Language);
        public uint       LowPc    => GetAttribute<uint>(Dwarf2Attribute.LowPc);
        public uint       HighPc   => GetAttribute<uint>(Dwarf2Attribute.HighPc);
        public string     Name     => GetAttribute<string>(Dwarf2Attribute.Name);
        public string     Producer => GetAttribute<string>(Dwarf2Attribute.Producer);
        public string     CompDir  => GetAttribute<string>(Dwarf2Attribute.CompDir);

        public Dwarf2CompileUnit(Dictionary<Dwarf2Attribute, object> attributes)
            : base(Dwarf2Tag.CompileUnit, attributes)
        {
        }

        public override string ToString() => Name;
    }
}