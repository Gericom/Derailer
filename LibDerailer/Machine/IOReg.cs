using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDerailer.Machine
{
    public class IOReg
    {
        public string Name = "Untitled";
        public UInt32 Bitsize = 32;
        public bool CanRead = true;
        public bool CanWrite = true;
        public string Category = "Ungrouped";
        public List<IOField> Fields = new List<IOField>();

        public override string ToString()
        {
            string perms = (CanRead ? "R" : "") + (CanWrite ? "W" : "");
            return $"IO Register {Category}::{Name} ({Bitsize}-bit): ({perms}) {Fields.Count} Fields";
        }
    }
}
