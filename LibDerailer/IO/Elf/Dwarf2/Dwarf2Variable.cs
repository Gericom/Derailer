using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.IO.Elf.Dwarf2.Enums;

namespace LibDerailer.IO.Elf.Dwarf2
{
    public class Dwarf2Variable : Dwarf2Die
    {
        public uint      DeclLine => Convert.ToUInt32(GetAttribute<object>(Dwarf2Attribute.DeclLine));
        public uint      DeclFile => Convert.ToUInt32(GetAttribute<object>(Dwarf2Attribute.DeclFile));
        public Dwarf2Die Type     => GetAttribute<Dwarf2Die>(Dwarf2Attribute.Type);
        public bool      External => GetAttribute<bool>(Dwarf2Attribute.External);
        public string    Name     => GetAttribute<string>(Dwarf2Attribute.Name);

        public Dwarf2Variable(Dictionary<Dwarf2Attribute, object> attributes) 
            : base(Dwarf2Tag.Variable, attributes)
        {
        }
    }
}
