namespace LibDerailer.IO.Elf.Dwarf2.Enums
{
    public enum Dwarf2Language
    {
        C89           = 0x0001,
        C             = 0x0002,
        Ada83         = 0x0003,
        CPlusPlus     = 0x0004,
        Cobol74       = 0x0005,
        Cobol85       = 0x0006,
        Fortran77     = 0x0007,
        Fortran90     = 0x0008,
        Pascal83      = 0x0009,
        Modula2       = 0x000a,
        Java          = 0x000b /* DWARF3 */,
        C99           = 0x000c /* DWARF3 */,
        Ada95         = 0x000d /* DWARF3 */,
        Fortran95     = 0x000e /* DWARF3 */,
        Pli           = 0x000f /* DWARF3 */,
        ObjC          = 0x0010 /* DWARF3f */,
        ObjCPlusPlus  = 0x0011 /* DWARF3f */,
        UPC           = 0x0012 /* DWARF3f */,
        D             = 0x0013 /* DWARF3f */,
        LoUser        = 0x8000,
        MipsAssembler = 0x8001 /* MIPS   */,

        Upc = 0x8765 /* UPC, use DW_LANG_UPC instead. */

/* ALTIUM extension */,
        AltiumAssembler = 0x9101 /* ALTIUM */
/* Sun extensions */,
        SunAssembler = 0x9001 /* SUN */,
        HiUser       = 0xffff
    }
}