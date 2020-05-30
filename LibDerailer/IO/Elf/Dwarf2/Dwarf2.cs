using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.IO.Elf.Dwarf2.Enums;
using LibDerailer.IO.Elf.Sections;

namespace LibDerailer.IO.Elf.Dwarf2
{
    public class Dwarf2
    {
        public Dwarf2Die[] RootDies { get; }

        private struct Dwarf2Fixup
        {
            public uint DieAddress { get; }

            public Dwarf2Fixup(uint dieAddress)
            {
                DieAddress = dieAddress;
            }
        }

        private struct RelocEntry
        {
            public uint                Offset { get; set; }
            public ElfSymtab.ElfSymbol Symbol { get; set; }
        }

        public Dwarf2(Elf elf)
        {
            var abbrev = elf.GetSectionByName(".debug_abbrev");
            if (abbrev == null)
                return;
            var info = elf.GetSectionByName(".debug_info");
            if (info == null)
                return;
            var infoRelocs = elf.GetSectionByName(".rel.debug_info");
            if (infoRelocs == null || infoRelocs.SectionHeader.EntrySize != 12)
                return;
            var symTab = elf.GetSectionByName(".symtab") as ElfSymtab;
            if (symTab == null)
                return;

            var relocs = new RelocEntry[infoRelocs.SectionHeader.FileImageSize / infoRelocs.SectionHeader.EntrySize];
            using (var reader = new EndianBinaryReader(new MemoryStream(infoRelocs.SectionHeader.SectionData),
                Endianness.LittleEndian))
            {
                for (int i = 0; i < relocs.Length; i++)
                {
                    relocs[i].Offset = reader.ReadUInt32();
                    uint entry = reader.ReadUInt32() >> 8;
                    relocs[i].Symbol = symTab.Symbols[(int) entry];
                    reader.ReadUInt32();
                }
            }
            var rootDies = new List<Dwarf2Die>();
            using (var dieReader = new EndianBinaryReader(new MemoryStream(info.SectionHeader.SectionData),
                Endianness.LittleEndian))
            {
                while (dieReader.BaseStream.Position < dieReader.BaseStream.Length)
                {
                    uint   unitSize     = dieReader.ReadUInt32();
                    ushort dwarfVersion = dieReader.ReadUInt16();
                    if (dwarfVersion != 2)
                        throw new Exception();
                    uint abbrevOffset = dieReader.ReadUInt32();
                    byte ptrSize      = dieReader.ReadByte();

                    var dieTypes = new Dictionary<uint, Dwarf2DieType>();
                    using (var abbrevReader = new EndianBinaryReader(new MemoryStream(abbrev.SectionHeader.SectionData),
                        Endianness.LittleEndian))
                    {
                        abbrevReader.BaseStream.Position = abbrevOffset;
                        while (true)
                        {
                            var dieType = new Dwarf2DieType(abbrevReader);
                            if (dieType.Id == 0)
                                break;
                            dieTypes.Add(dieType.Id, dieType);
                        }
                    }

                    var  stack    = new Stack<Dwarf2Die>();
                    var  dies     = new Dictionary<uint, Dwarf2Die>();
                    long end      = dieReader.BaseStream.Position + unitSize - 7;
                    int  level    = 0;
                    while (dieReader.BaseStream.Position < end)
                    {
                        uint addr = (uint) dieReader.BaseStream.Position;
                        uint type = dieReader.ReadUnsignedLeb128();
                        if (type == 0)
                        {
                            stack.Pop();
                            continue;
                        }

                        var dieType = dieTypes[type];
                        var attribs = new Dictionary<Dwarf2Attribute, object>();
                        foreach (var (attrib, form) in dieType.Attributes)
                        {
                            switch (form)
                            {
                                case Dwarf2Form.Udata:
                                    attribs.Add(attrib, dieReader.ReadUnsignedLeb128());
                                    break;
                                case Dwarf2Form.RefAddr:
                                    int idx = Array.FindIndex(relocs, r => r.Offset == dieReader.BaseStream.Position);
                                    if (attrib == Dwarf2Attribute.Type && idx >= 0)
                                    {
                                        dieReader.ReadUInt32();
                                        attribs.Add(attrib, new Dwarf2Fixup(relocs[idx].Symbol.Value));
                                    }
                                    else
                                        attribs.Add(attrib, dieReader.ReadUInt32());

                                    break;
                                case Dwarf2Form.Data1:
                                    attribs.Add(attrib, dieReader.ReadByte());
                                    break;
                                case Dwarf2Form.Data2:
                                    attribs.Add(attrib, dieReader.ReadUInt16());
                                    break;
                                case Dwarf2Form.Data4:
                                    attribs.Add(attrib, dieReader.ReadUInt32());
                                    break;
                                case Dwarf2Form.String:
                                    attribs.Add(attrib, dieReader.ReadStringNT(Encoding.UTF8));
                                    break;
                                case Dwarf2Form.Block2:
                                    attribs.Add(attrib, dieReader.ReadBytes(dieReader.ReadUInt16()));
                                    break;
                                case Dwarf2Form.Flag:
                                    attribs.Add(attrib, dieReader.ReadByte() != 0);
                                    break;
                                default:
                                    throw new NotImplementedException();
                            }
                        }

                        Dwarf2Die die;

                        switch (dieType.Tag)
                        {
                            case Dwarf2Tag.CompileUnit:
                                die = new Dwarf2CompileUnit(attribs);
                                break;
                            case Dwarf2Tag.Typedef:
                                die = new Dwarf2Typedef(attribs);
                                break;
                            case Dwarf2Tag.Subprogram:
                                die = new Dwarf2Subprogram(attribs);
                                break;
                            case Dwarf2Tag.BaseType:
                                die = new Dwarf2BaseType(attribs);
                                break;
                            case Dwarf2Tag.PointerType:
                                die = new Dwarf2PointerType(attribs);
                                break;
                            case Dwarf2Tag.StructureType:
                                die = new Dwarf2StructureType(attribs);
                                break;
                            case Dwarf2Tag.UnionType:
                                die = new Dwarf2UnionType(attribs);
                                break;
                            case Dwarf2Tag.ClassType:
                                die = new Dwarf2ClassType(attribs);
                                break;
                            case Dwarf2Tag.Member:
                                die = new Dwarf2Member(attribs);
                                break;
                            case Dwarf2Tag.Inheritance:
                                die = new Dwarf2Inheritance(attribs);
                                break;
                            default:
                                die = new Dwarf2Die(dieType.Tag, attribs);
                                break;
                        }

                        dies.Add(addr, die);
                        if (stack.Count > 0)
                            stack.Peek().Children.Add(die);
                        else
                            rootDies.Add(die);

                        if (dieType.HasChildren)
                            stack.Push(die);
                    }

                    foreach (var die in dies.Values)
                    {
                        var fixups = die.Attributes.Where(kv => kv.Value is Dwarf2Fixup).ToArray();
                        foreach (var fixup in fixups)
                            die.Attributes[fixup.Key] = dies[((Dwarf2Fixup) fixup.Value).DieAddress];
                    }
                }
            }

            RootDies = rootDies.ToArray();
        }
    }
}