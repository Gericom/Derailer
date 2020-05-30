using System;

namespace LibDerailer.IO.Elf.Sections
{
    public class ElfSection
    {
        public Elf.SectionHeaderTableEntry SectionHeader { get; }
        public String Name { get; }

        protected ElfSection(Elf.SectionHeaderTableEntry sectionHeader, string name)
        {
            SectionHeader = sectionHeader;
            Name = name;
        }

        public static ElfSection CreateInstance(Elf.SectionHeaderTableEntry section, string name)
        {
            switch (section.SectionType)
            {
                case Elf.SectionHeaderTableEntry.ElfSectionType.Strtab:
                    return new ElfStrtab(section, name);
                case Elf.SectionHeaderTableEntry.ElfSectionType.Symtab:
                    return new ElfSymtab(section, name);
                default:
                    return new ElfSection(section, name);
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
