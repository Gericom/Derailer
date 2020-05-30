using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.IO.Elf.Dwarf2.Enums;

namespace LibDerailer.IO.Elf.Dwarf2
{
    public class Dwarf2Inheritance : Dwarf2Die
    {
        public Dwarf2Accessibility Accessibility
            => (Dwarf2Accessibility) GetAttribute<byte>(Dwarf2Attribute.Accessibility);

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

        public Dwarf2Die        Type       => GetAttribute<Dwarf2Die>(Dwarf2Attribute.Type);
        public Dwarf2Virtuality Virtuality => (Dwarf2Virtuality) GetAttribute<byte>(Dwarf2Attribute.Virtuality);

        public Dwarf2Inheritance(Dictionary<Dwarf2Attribute, object> attributes)
            : base(Dwarf2Tag.Inheritance, attributes)
        {
        }
    }
}