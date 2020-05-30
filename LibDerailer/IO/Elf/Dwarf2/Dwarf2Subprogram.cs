using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.IO.Elf.Dwarf2.Enums;

namespace LibDerailer.IO.Elf.Dwarf2
{
    public class Dwarf2Subprogram : Dwarf2Die
    {
        public uint      LowPc    => GetAttribute<uint>(Dwarf2Attribute.LowPc);
        public uint      HighPc   => GetAttribute<uint>(Dwarf2Attribute.HighPc);
        public uint      DeclLine => GetAttribute<uint>(Dwarf2Attribute.DeclLine);
        public uint      DeclFile => GetAttribute<uint>(Dwarf2Attribute.DeclFile);
        public Dwarf2Die Type     => GetAttribute<Dwarf2Die>(Dwarf2Attribute.Type);
        public bool      External => GetAttribute<bool>(Dwarf2Attribute.External);
        public string    Name     => GetAttribute<string>(Dwarf2Attribute.Name);

        public Dwarf2Subprogram(Dictionary<Dwarf2Attribute, object> attributes)
            : base(Dwarf2Tag.Subprogram, attributes)
        {
        }

        public override string ToString() => Name;
    }
}