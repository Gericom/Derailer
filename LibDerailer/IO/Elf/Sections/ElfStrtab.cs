namespace LibDerailer.IO.Elf.Sections
{
    public class ElfStrtab : ElfSection
    {
        public ElfStrtab(Elf.SectionHeaderTableEntry section, string name)
            : base(section, name)
        { }

        public string GetString(uint offset)
        {
            string cur = "";
            while (offset < SectionHeader.SectionData.Length)
            {
                char c = (char)SectionHeader.SectionData[offset++];
                if (c == '\0')
                    return cur;
                cur += c;
            }
            return null;
        }
    }
}
