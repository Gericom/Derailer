namespace LibDerailer.IO.Elf.Dwarf2.Enums
{
    public enum Dwarf2Tag
    {
        ArrayType             = 0x01,
        ClassType             = 0x02,
        EntryPoint            = 0x03,
        EnumerationType       = 0x04,
        FormalParameter       = 0x05,
        ImportedDeclaration   = 0x08,
        Label                 = 0x0a,
        LexicalBlock          = 0x0b,
        Member                = 0x0d,
        PointerType           = 0x0f,
        ReferenceType         = 0x10,
        CompileUnit           = 0x11,
        StringType            = 0x12,
        StructureType         = 0x13,
        SubroutineType        = 0x15,
        Typedef               = 0x16,
        UnionType             = 0x17,
        UnspecifiedParameters = 0x18,
        Variant               = 0x19,
        CommonBlock           = 0x1a,
        CommonInclusion       = 0x1b,
        Inheritance           = 0x1c,
        InlinedSubroutine     = 0x1d,
        Module                = 0x1e,
        PtrToMemberType       = 0x1f,
        SetType               = 0x20,
        SubrangeType          = 0x21,
        WithStmt              = 0x22,
        AccessDeclaration     = 0x23,
        BaseType              = 0x24,
        CatchBlock            = 0x25,
        ConstType             = 0x26,
        Constant              = 0x27,
        Enumerator            = 0x28,
        FileType              = 0x29,
        Friend                = 0x2a,

        Namelist = 0x2b
        /* Early releases of this header had the following
           misspelled with a trailing 's' */,
        NamelistItem  = 0x2c /* DWARF3/2 spelling */,
        NamelistItems = 0x2c /* SGI misspelling/typo */,
        PackedType    = 0x2d,

        Subprogram = 0x2e
        /* The DWARF2 document had two spellings of the following
           two TAGs, DWARF3 specifies the longer spelling. */,
        TemplateTypeParameter  = 0x2f /* DWARF3/2 spelling*/,
        TemplateTypeParam      = 0x2f /* DWARF2   spelling*/,
        TemplateValueParameter = 0x30 /* DWARF3/2 spelling*/,
        TemplateValueParam     = 0x30 /* DWARF2   spelling*/,
        ThrownType             = 0x31,
        TryBlock               = 0x32,
        VariantPart            = 0x33,
        Variable               = 0x34,
        VolatileType           = 0x35,
        DwarfProcedure         = 0x36 /* DWARF3 */,
        RestrictType           = 0x37 /* DWARF3 */,
        InterfaceType          = 0x38 /* DWARF3 */,
        Namespace              = 0x39 /* DWARF3 */,
        ImportedModule         = 0x3a /* DWARF3 */,
        UnspecifiedType        = 0x3b /* DWARF3 */,
        PartialUnit            = 0x3c /* DWARF3 */,
        ImportedUnit           = 0x3d /* DWARF3 */
        /* Do not use DW_TAG_mutable_type */,
        MutableType         = 0x3e /* Withdrawn from DWARF3 by DWARF3f. */,
        Condition           = 0x3f /* DWARF3f */,
        SharedType          = 0x40 /* DWARF3f */,
        TypeUnit            = 0x41 /* DWARF4 */,
        RvalueReferenceType = 0x42 /* DWARF4 */,
        LoUser              = 0x4080,
        MipsLoop            = 0x4081

        /* HP extensions: ftp://ftp.hp.com/pub/lang/tools/WDB/wdb-4.0.tar.gz  */,
        HpArrayDescriptor = 0x4090 /* HP */

        /* GNU extensions.  The first 3 missing the GNU_. */,
        FormatLabel      = 0x4101 /* GNU. Fortran. */,
        FunctionTemplate = 0x4102 /* GNU. For C++ */,
        ClassTemplate    = 0x4103 /* GNU. For C++ */,
        GnuBincl         = 0x4104 /* GNU */,
        GnuEincl         = 0x4105 /* GNU */

        /* ALTIUM extensions */
        /* DSP-C/Starcore __circ qualifier */,
        AltiumCircType = 0x5101 /* ALTIUM */
        /* Starcore __mwa_circ qualifier */,
        AltiumMwaCircType = 0x5102 /* ALTIUM */
        /* Starcore __rev_carry qualifier */,
        AltiumRevCarryType = 0x5103 /* ALTIUM */
        /* M16 __rom qualifier */,
        AltiumRom = 0x5111 /* ALTIUM */

        /* The following 3 are extensions to support UPC */,
        UpcSharedType  = 0x8765 /* UPC */,
        UpcStrictType  = 0x8766 /* UPC */,
        UpcRelaxedType = 0x8767 /* UPC */

        /* PGI (STMicroelectronics) extensions. */,
        PgiKanjiType      = 0xa000 /* PGI */,
        PgiInterfaceBlock = 0xa020 /* PGI */
        /* The following are SUN extensions */,
        SunFunctionTemplate    = 0x4201 /* SUN */,
        SunClassTemplate       = 0x4202 /* SUN */,
        SunStructTemplate      = 0x4203 /* SUN */,
        SunUnionTemplate       = 0x4204 /* SUN */,
        SunIndirectInheritance = 0x4205 /* SUN */,
        SunCodeflags           = 0x4206 /* SUN */,
        SunMemopInfo           = 0x4207 /* SUN */,
        SunOmpChildFunc        = 0x4208 /* SUN */,
        SunRttiDescriptor      = 0x4209 /* SUN */,
        SunDtorInfo            = 0x420a /* SUN */,
        SunDtor                = 0x420b /* SUN */,
        SunF90Interface        = 0x420c /* SUN */,
        SunFortranVaxStructure = 0x420d /* SUN */,
        SunHi                  = 0x42ff /* SUN */,
        HiUser                 = 0xffff
    }
}