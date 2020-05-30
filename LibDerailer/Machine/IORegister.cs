using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDerailer.Machine
{
    public class IORegister
    {
        /// <summary>
        /// The category of this IO register
        /// </summary>
        public string Category { get; }

        /// <summary>
        /// The name of this IO register
        /// </summary>
        public string Name     { get; }

        /// <summary>
        /// The physical address of this IO register
        /// </summary>
        public uint   Address  { get; }

        /// <summary>
        /// The size of this IO register in bits
        /// </summary>
        public uint   BitSize  { get; }

        /// <summary>
        /// Whether this IO register is readable
        /// </summary>
        public bool   Readable { get; }

        /// <summary>
        /// Whether this IO register is writable
        /// </summary>
        public bool   Writable { get; }

        /// <summary>
        /// The bit fields of this IO register
        /// </summary>
        public IReadOnlyList<IORegisterField> Fields { get; }

        public IORegister(string category, string name, uint address, uint bitSize, bool readable, bool writable,
            params IORegisterField[] fields)
        {
            Category = category;
            Name     = name;
            Address  = address;
            BitSize  = bitSize;
            Readable = readable;
            Writable = writable;
            Fields   = fields;
        }

        public IORegisterField GetField(uint offset, uint size) 
            => Fields.FirstOrDefault(f => f.BitOffset == offset && f.BitSize == size);

        public override string ToString()
        {
            string perms = (Readable ? "R" : "") + (Writable ? "W" : "");
            return $"IO Register {Category}::{Name} ({BitSize}-bit): ({perms}) {Fields.Count} Fields";
        }
    }
}