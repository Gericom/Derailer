using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDerailer.CodeGraph
{
    public class Function
    {
        public uint   Address { get; }
        public string Name    { get; }

        public List<BasicBlock> BasicBlocks { get; } = new List<BasicBlock>();

        public Function(uint address)
        {
            Address = address;
        }
    }
}