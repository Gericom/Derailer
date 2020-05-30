namespace LibDerailer.IO.Elf.Dwarf2.Enums
{
    public enum Dwarf2Encoding
    {
        Address        = 0x1,
        Boolean        = 0x2,
        ComplexFloat   = 0x3,
        Float          = 0x4,
        Signed         = 0x5,
        SignedChar     = 0x6,
        Unsigned       = 0x7,
        UnsignedChar   = 0x8,
        ImaginaryFloat = 0x9 /* DWARF3 */,
        PackedDecimal  = 0xa /* DWARF3f */,
        NumericString  = 0xb /* DWARF3f */,
        Edited         = 0xc /* DWARF3f */,
        SignedFixed    = 0xd /* DWARF3f */,
        UnsignedFixed  = 0xe /* DWARF3f */,
        DecimalFloat   = 0xf /* DWARF3f */


        /* ALTIUM extensions. x80, x81 */,
        AltiumFract = 0x80 /* ALTIUM __fract type */

        /* Follows extension so dwarfdump prints the most-likely-useful name. */,
        LoUser = 0x80

        /* Shown here to help dwarfdump build script. */,
        AltiumAccum = 0x81 /* ALTIUM __accum type */

        /* HP Floating point extensions. */,
        HpFloat80           = 0x80 /* (80 bit). HP */,
        HpComplexFloat80    = 0x81 /* Complex (80 bit). HP  */,
        HpFloat128          = 0x82 /* (128 bit). HP */,
        HpComplexFloat128   = 0x83 /* Complex (128 bit). HP */,
        HpFloathpintel      = 0x84 /* (82 bit IA64). HP */,
        HpImaginaryFloat80  = 0x85 /* HP */,
        HpImaginaryFloat128 = 0x86 /* HP */

        /* Sun extensions */,
        SunIntervalFloat  = 0x91,
        SunImaginaryFloat = 0x92 /* Obsolete: See DW_ATE_imaginary_float */,
        HiUser            = 0xff
    }
}