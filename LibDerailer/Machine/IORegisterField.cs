using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDerailer.Machine
{
    public class IORegisterField
    {
        /// <summary>
        /// The name of this field
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The bit offset of this field
        /// </summary>
        public uint BitOffset { get; }

        /// <summary>
        /// The size of this field in bits
        /// </summary>
        public uint BitSize { get; }

        public IORegisterField(string name, uint bitOffset, uint bitSize)
        {
            Name    = name;
            BitOffset   = bitOffset;
            BitSize = bitSize;
        }
    }
}