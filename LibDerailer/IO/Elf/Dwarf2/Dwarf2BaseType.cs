using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.IO.Elf.Dwarf2.Enums;

namespace LibDerailer.IO.Elf.Dwarf2
{
    public class Dwarf2BaseType : Dwarf2Die
    {
        public uint      ByteSize => Convert.ToUInt32(GetAttribute<object>(Dwarf2Attribute.ByteSize));
        public Dwarf2Encoding Encoding => (Dwarf2Encoding) GetAttribute<byte>(Dwarf2Attribute.Encoding);
        public string    Name     => GetAttribute<string>(Dwarf2Attribute.Name);

        public Dwarf2BaseType(Dictionary<Dwarf2Attribute, object> attributes)
            : base(Dwarf2Tag.BaseType, attributes)
        {
        }

        public override string ToString() => Name;
    }
}