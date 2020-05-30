using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;

namespace LibDerailer.Machine
{
    public abstract class MemoryMap
    {
        // Search for the lower bound, as regs may be 8 bytes long
        private readonly Dictionary<(uint address, uint size), IORegister> _map =
            new Dictionary<(uint address, uint size), IORegister>();

        public IReadOnlyCollection<IORegister> Registers => _map.Values;

        protected MemoryMap(string path, uint baseAddress)
        {
            // https://stackoverflow.com/a/20523165
            using (var parser = new TextFieldParser(path))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                while (!parser.EndOfData)
                {
                    var fields = parser.ReadFields();
                    if (fields[0].StartsWith("#"))
                        continue;

                    uint addr = Convert.ToUInt32(fields[0], 16);
                    if (addr < baseAddress)
                        addr += baseAddress;

                    var bitFields = new List<IORegisterField>();
                    // Start at 7
                    for (int i = 7; i < fields.Length; i += 3)
                    {
                        var field = fields.Skip(i).Take(3).ToArray();
                        if (field[0].Length == 0 || field[1].Length == 0 || field[2].Length == 0)
                            break;
                        bitFields.Add(
                            new IORegisterField(field[0], uint.Parse(field[1]), uint.Parse(field[2])));
                    }

                    var reg = new IORegister(
                        fields[5],
                        fields[2],
                        addr,
                        uint.Parse(fields[3]),
                        fields[4].Contains("r"),
                        fields[4].Contains("w"),
                        bitFields.ToArray());

                    _map.Add((addr, reg.BitSize / 8), reg);
                }
            }
        }

        public IORegister GetRegister(uint address, uint size) 
            => _map.TryGetValue((address, size), out var reg) ? reg : null;

        public abstract string GetRegisterDefineName(IORegister register);
        public abstract string GetRegisterOffsetDefineName(IORegister register);
        public abstract string GetRegisterAddressDefineName(IORegister register);
        public abstract string GetRegisterFieldShiftDefineName(IORegister register, IORegisterField field);
        public abstract string GetRegisterFieldSizeDefineName(IORegister register, IORegisterField field);
        public abstract string GetRegisterFieldMaskDefineName(IORegister register, IORegisterField field);
    }
}