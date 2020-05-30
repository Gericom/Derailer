using System.Collections.Generic;
using System.IO;

namespace LibDerailer.IO.Elf.Sections
{
    public class ElfSymtab : ElfSection
    {
        private readonly ElfSymbol[] _symbols;

        public IReadOnlyList<ElfSymbol> Symbols => _symbols;

        public ElfSymtab(Elf.SectionHeaderTableEntry section, string name)
            : base(section, name)
        {
            uint nrEntries = section.FileImageSize / section.EntrySize;
            _symbols = new ElfSymbol[nrEntries];
            var er = new EndianBinaryReader(new MemoryStream(section.SectionData), Endianness.LittleEndian);
            for(int i = 0; i < nrEntries; i++)
                _symbols[i] = new ElfSymbol(er);
            er.Close();
        }

        public class ElfSymbol
        {
            public ElfSymbol(EndianBinaryReader er)
            {
                NameOffset = er.ReadUInt32();
                Value = er.ReadUInt32();
                Size = er.ReadUInt32();
                Info = er.ReadByte();
                Other = er.ReadByte();
                SectionIndex = er.ReadUInt16();
            }

            public uint NameOffset;
            public uint Value;
            public uint Size;
            public byte Info;
            public byte Other;
            public ushort SectionIndex;
        }
    }
}
