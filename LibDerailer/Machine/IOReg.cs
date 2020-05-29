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
        public string category = "Ungrouped";
        public List<IOField> Fields = new List<IOField>();
    }
}
