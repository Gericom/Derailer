using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.IO.Elf.Dwarf2.Enums;

namespace LibDerailer.IO.Elf.Dwarf2
{
    public class Dwarf2DieType
    {
        public uint      Id          { get; }
        public Dwarf2Tag Tag         { get; }
        public bool      HasChildren { get; }

        public IReadOnlyList<(Dwarf2Attribute attribute, Dwarf2Form form)> Attributes { get; }

        public Dwarf2DieType(EndianBinaryReader reader)
        {
            Id          = reader.ReadUnsignedLeb128();
            if (Id == 0)
                return;
            Tag         = (Dwarf2Tag) reader.ReadByte();
            HasChildren = reader.ReadByte() == 1;
            var attrs = new List<(Dwarf2Attribute, Dwarf2Form)>();
            while (true)
            {
                var attr = (Dwarf2Attribute) reader.ReadByte();
                var form = (Dwarf2Form) reader.ReadByte();
                if (attr == 0)
                    break;
                attrs.Add((attr, form));
            }

            Attributes = attrs.ToArray();
        }
    }
}