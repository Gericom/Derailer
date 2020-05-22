using System.Linq;

namespace LibDerailer.CodeGraph.Nodes
{
    public class LoadConstant : Instruction
    {
        public uint Constant { get; }

        public LoadConstant(Variable dst, uint constant)
        {
            Constant = constant;
            VariableDefs.Add(dst);
        }

        public override string ToString() => $"{VariableDefs.First()} = 0x{Constant:X08}";
    }
}