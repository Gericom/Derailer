using System.IO;
using System.Linq;
using LibDerailer.IO.Elf.Sections;

namespace LibDerailer.IO.Elf
{
    public class Elf
    {
        public Elf(byte[] data)
        {
            var er = new EndianBinaryReader(new MemoryStream(data), Endianness.LittleEndian);
            Header = new ElfHeader(er);

            er.BaseStream.Position = Header.ProgramHeaderTableOffset;
            ProgramHeaderTable = new ProgramHeaderTableEntry[Header.ProgramHeaderTableEntryCount];
            for (int i = 0; i < Header.ProgramHeaderTableEntryCount; i++)
                ProgramHeaderTable[i] = new ProgramHeaderTableEntry(er);

            er.BaseStream.Position = Header.SectionHeaderTableOffset;
            SectionHeaderTable = new SectionHeaderTableEntry[Header.SectionHeaderTableEntryCount];
            for (int i = 0; i < Header.SectionHeaderTableEntryCount; i++)
                SectionHeaderTable[i] = new SectionHeaderTableEntry(er);

            er.Close();

            var namestab = new ElfStrtab(SectionHeaderTable[Header.SectionNamesIndex], null);
            Sections = new ElfSection[SectionHeaderTable.Length];
            for (int i = 0; i < SectionHeaderTable.Length; i++)
                Sections[i] = ElfSection.CreateInstance(SectionHeaderTable[i],
                    namestab.GetString(SectionHeaderTable[i].NameOffset));
        }

        public ElfHeader Header;
        public class ElfHeader
        {
            private const uint ElfHeaderMagic = 0x464C457F;

            public ElfHeader(EndianBinaryReader er)
            {
                Magic = er.ReadUInt32();
                if (Magic != ElfHeaderMagic)
                    throw new InvalidDataException("Elf magic invalid!");
                BitFormat = er.ReadByte();
                Endianness = er.ReadByte();
                Version = er.ReadByte();
                Abi = er.ReadByte();
                AbiVersion = er.ReadByte();
                Padding = er.ReadBytes(7);
                ObjectType = er.ReadUInt16();
                Architecture = er.ReadUInt16();
                Version2 = er.ReadUInt32();
                EntryPoint = er.ReadUInt32();
                ProgramHeaderTableOffset = er.ReadUInt32();
                SectionHeaderTableOffset = er.ReadUInt32();
                Flags = er.ReadUInt32();
                HeaderSize = er.ReadUInt16();
                ProgramHeaderTableEntrySize = er.ReadUInt16();
                ProgramHeaderTableEntryCount = er.ReadUInt16();
                SectionHeaderTableEntrySize = er.ReadUInt16();
                SectionHeaderTableEntryCount = er.ReadUInt16();
                SectionNamesIndex = er.ReadUInt16();
            }

            public uint Magic;
            public byte BitFormat;
            public byte Endianness;
            public byte Version;
            public byte Abi;
            public byte AbiVersion;
            public byte[] Padding;//7
            public ushort ObjectType;
            public ushort Architecture;
            public uint Version2;
            public uint EntryPoint;
            public uint ProgramHeaderTableOffset;
            public uint SectionHeaderTableOffset;
            public uint Flags;
            public ushort HeaderSize;
            public ushort ProgramHeaderTableEntrySize;
            public ushort ProgramHeaderTableEntryCount;
            public ushort SectionHeaderTableEntrySize;
            public ushort SectionHeaderTableEntryCount;
            public ushort SectionNamesIndex;
        }

        public ProgramHeaderTableEntry[] ProgramHeaderTable;
        public class ProgramHeaderTableEntry
        {
            public enum ElfSegmentType : uint
            {
                Null = 0,
                Load = 1,
                Dynamic = 2,
                Interp = 3,
                Note = 4,
                Shlib = 5,
                Phdr = 6,
                Tls = 7,
                Num = 8,
                Loos = 0x60000000,
                Hios = 0x6fffffff,
                Loproc = 0x70000000,
                Hiproc = 0x7fffffff
            }

            public ProgramHeaderTableEntry(EndianBinaryReader er)
            {
                SegmentType = (ElfSegmentType)er.ReadUInt32();
                FileImageOffset = er.ReadUInt32();
                VirtualAddress = er.ReadUInt32();
                PhysicalAddress = er.ReadUInt32();
                FileImageSize = er.ReadUInt32();
                MemorySize = er.ReadUInt32();
                Flags = er.ReadUInt32();
                Alignment = er.ReadUInt32();

                if (FileImageSize != 0)
                {
                    long curpos = er.BaseStream.Position;
                    er.BaseStream.Position = FileImageOffset;
                    SegmentData = er.ReadBytes((int)FileImageSize);
                    er.BaseStream.Position = curpos;
                }
            }

            public ElfSegmentType SegmentType;
            public uint FileImageOffset;
            public uint VirtualAddress;
            public uint PhysicalAddress;
            public uint FileImageSize;
            public uint MemorySize;
            public uint Flags;
            public uint Alignment;

            public byte[] SegmentData;
        }

        public SectionHeaderTableEntry[] SectionHeaderTable;
        public class SectionHeaderTableEntry
        {
            public enum ElfSectionType : uint
            {
                Null = 0,
                Progbits = 1,
                Symtab = 2,
                Strtab = 3,
                Rela = 4,
                Hash = 5,
                Dynamic = 6,
                Note = 7,
                Nobits = 8,
                Rel = 9,
                Shlib = 10,
                Dynsym = 11,
                InitArray = 14,
                FiniArray = 15,
                PreinitArray = 16,
                Group = 17,
                SymtabShndx = 18,
                Num = 19,
                Loos = 0x60000000,
                Hios = 0x6fffffff,
                Loproc = 0x70000000,
                Hiproc = 0x7fffffff,
                Louser = 0x80000000,
                Hiuser = 0xffffffff
            }

            public SectionHeaderTableEntry(EndianBinaryReader er)
            {
                NameOffset = er.ReadUInt32();
                SectionType = (ElfSectionType)er.ReadUInt32();
                Flags = er.ReadUInt32();
                VirtualAddress = er.ReadUInt32();
                FileImageOffset = er.ReadUInt32();
                FileImageSize = er.ReadUInt32();
                Link = er.ReadUInt32();
                Info = er.ReadUInt32();
                Alignment = er.ReadUInt32();
                EntrySize = er.ReadUInt32();

                if(FileImageSize != 0)
                {
                    long curpos = er.BaseStream.Position;
                    er.BaseStream.Position = FileImageOffset;
                    SectionData = er.ReadBytes((int)FileImageSize);
                    er.BaseStream.Position = curpos;
                }
            }

            public uint NameOffset;
            public ElfSectionType SectionType;
            public uint Flags;
            public uint VirtualAddress;
            public uint FileImageOffset;
            public uint FileImageSize;
            public uint Link;
            public uint Info;
            public uint Alignment;
            public uint EntrySize;

            public byte[] SectionData;
        }

        public ElfSection[] Sections { get; }

        public ElfSection GetSectionByName(string name)
            => (from s in Sections where s.Name == name select s).FirstOrDefault();
    }
}
