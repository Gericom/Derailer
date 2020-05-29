using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;

namespace LibDerailer.Machine
{
    public class MemoryMap
    {
        // Search for the lower bound, as regs may be 8 bytes long
        public Dictionary<UInt32, IOReg> Map = new Dictionary<uint, IOReg>();

        public MemoryMap(string path, UInt32 base_address = 0x04000000)
        {
            // https://stackoverflow.com/a/20523165
            using (TextFieldParser parser = new TextFieldParser(path))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                while (!parser.EndOfData)
                {
                    string[] fields = parser.ReadFields();
                    if (fields[0].StartsWith("#"))
                        continue;

                    UInt32 addr = Convert.ToUInt32(fields[0], 16);
                    if (addr < base_address) addr += base_address;
                    var reg = new IOReg();
                    reg.Name = fields[2];
                    reg.Bitsize = Convert.ToUInt32(fields[3], 10);
                    reg.CanRead = fields[4].Contains("r");
                    reg.CanWrite = fields[4].Contains("w");
                    reg.category = fields[5];
                    // Start at 7
                    for (int i = 7; i < fields.Length; i += 3)
                    {
                        var field = fields.Skip(i).Take(3).ToArray();
                        if (field[0].Length == 0 || field[1].Length == 0 || field[2].Length == 0)
                            break;
                        reg.Fields.Add(new IOField
                        {
                            Name = field[0],
                            Shift = Convert.ToUInt32(field[1], 10),
                            Bit = Convert.ToUInt32(field[2], 10)
                        });
                    }
                    try
                    {
                        Map.Add(addr, reg);
                    }
                    catch (ArgumentException e)
                    {
                        // Ignore for now
                    }
                }
            }
        }
    }
}
