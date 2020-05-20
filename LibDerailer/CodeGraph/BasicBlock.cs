using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDerailer.CodeGraph
{
    public class BasicBlock : IComparable<BasicBlock>
    {
        public uint Address { get; }

        public List<Instruction> Instructions { get; } = new List<Instruction>();

        public List<BasicBlock> Predecessors { get; } = new List<BasicBlock>();
        public List<BasicBlock> Successors   { get; } = new List<BasicBlock>();

        public BasicBlock(uint address)
        {
            Address = address;
        }

        public int CompareTo(BasicBlock other)
        {
            if (ReferenceEquals(this, other))
                return 0;
            if (ReferenceEquals(null, other))
                return 1;
            return Address.CompareTo(other.Address);
        }
    }
}