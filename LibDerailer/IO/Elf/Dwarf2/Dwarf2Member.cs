using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.IO.Elf.Dwarf2.Enums;

namespace LibDerailer.IO.Elf.Dwarf2
{
    public class Dwarf2Member : Dwarf2Die
    {
        public uint DataMemberLocation
        {
            get
            {
                var data = GetAttribute<byte[]>(Dwarf2Attribute.DataMemberLocation);
                if ((Dwarf2Op) data[0] != Dwarf2Op.Const4U || data.Length != 5)
                    throw new Exception();
                return IOUtil.ReadU32Le(data, 1);
            }
        }

        public Dwarf2Die Type      => GetAttribute<Dwarf2Die>(Dwarf2Attribute.Type);
        public string    Name      => GetAttribute<string>(Dwarf2Attribute.Name);
        public uint      BitSize   => GetAttribute<uint>(Dwarf2Attribute.BitSize);
        public uint      BitOffset => GetAttribute<uint>(Dwarf2Attribute.BitOffset);

        public Dwarf2Member(Dictionary<Dwarf2Attribute, object> attributes)
            : base(Dwarf2Tag.Member, attributes)
        {
        }

        public override string ToString() => Name;
    }
}