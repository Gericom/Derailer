namespace LibDerailer.IO.Elf.Dwarf2.Enums
{
    public enum Dwarf2Form
    {
        Addr         = 0x01,
        Block2       = 0x03,
        Block4       = 0x04,
        Data2        = 0x05,
        Data4        = 0x06,
        Data8        = 0x07,
        String       = 0x08,
        Block        = 0x09,
        Block1       = 0x0a,
        Data1        = 0x0b,
        Flag         = 0x0c,
        Sdata        = 0x0d,
        Strp         = 0x0e,
        Udata        = 0x0f,
        RefAddr     = 0x10,
        Ref1         = 0x11,
        Ref2         = 0x12,
        Ref4         = 0x13,
        Ref8         = 0x14,
        RefUdata    = 0x15,
        Indirect     = 0x16,
        SecOffset   = 0x17 /* DWARF4 */,
        Exprloc      = 0x18 /* DWARF4 */,
        FlagPresent = 0x19 /* DWARF4 */,
        RefSig8     = 0x20 /* DWARF4 */
    }
}