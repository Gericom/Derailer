using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.IO.Elf.Dwarf2.Enums;

namespace LibDerailer.IO.Elf.Dwarf2
{
    public class Dwarf2FormalParameter : Dwarf2Die
    {
        public Dwarf2Die Type => GetAttribute<Dwarf2Die>(Dwarf2Attribute.Type);
        public string    Name => GetAttribute<string>(Dwarf2Attribute.Name);

        public Dwarf2FormalParameter(Dictionary<Dwarf2Attribute, object> attributes) 
            : base(Dwarf2Tag.FormalParameter, attributes)
        {
        }
    }
}
