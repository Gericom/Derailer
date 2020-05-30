using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.IO.Elf.Dwarf2.Enums;

namespace LibDerailer.IO.Elf.Dwarf2
{
    public class Dwarf2Die
    {
        public Dwarf2Tag Tag { get; }

        public Dictionary<Dwarf2Attribute, object> Attributes { get; }

        public List<Dwarf2Die> Children { get; } = new List<Dwarf2Die>();

        public Dwarf2Die(Dwarf2Tag tag, Dictionary<Dwarf2Attribute, object> attributes)
        {
            Tag        = tag;
            Attributes = attributes;
        }

        public T GetAttribute<T>(Dwarf2Attribute attrib)
        {
            if (Attributes.TryGetValue(attrib, out object val))
                return (T) val;
            return default;
        }
    }
}