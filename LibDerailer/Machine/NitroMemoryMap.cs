using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDerailer.Machine
{
    public class NitroMemoryMap : MemoryMap
    {
        public NitroMemoryMap(string path)
            : base(path, 0x04000000)
        {
        }

        public override string GetRegisterDefineName(IORegister register)
            => $"reg_{register.Category}_{register.Name}";

        public override string GetRegisterOffsetDefineName(IORegister register)
            => $"REG_{register.Name}_OFFSET";

        public override string GetRegisterAddressDefineName(IORegister register)
            => $"REG_{register.Name}_ADDR";

        public override string GetRegisterFieldShiftDefineName(IORegister register, IORegisterField field)
            => $"REG_{register.Category}_{register.Name}_{field.Name}_SHIFT";

        public override string GetRegisterFieldSizeDefineName(IORegister register, IORegisterField field)
            => $"REG_{register.Category}_{register.Name}_{field.Name}_SIZE";

        public override string GetRegisterFieldMaskDefineName(IORegister register, IORegisterField field)
            => $"REG_{register.Category}_{register.Name}_{field.Name}_MASK";
    }
}