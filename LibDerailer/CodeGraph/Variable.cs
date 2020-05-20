using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDerailer.CodeGraph
{
    public class Variable : IComparable<Variable>
    {
        public string Name { get; set; }

        public int Size { get; set; }

        public VariableLocation Location { get; }

        //regnum for register var
        //stack offset for stack var
        //memory address for memory var
        public int Address { get; }

        public Variable(VariableLocation location, string name, int address, int size)
        {
            Location = location;
            Name     = name;
            Size     = size;
            Address  = address;
        }

        public int CompareTo(Variable other)
        {
            if (ReferenceEquals(this, other))
                return 0;
            if (ReferenceEquals(null, other))
                return 1;
            return Address.CompareTo(other.Address);
        }
    }
}