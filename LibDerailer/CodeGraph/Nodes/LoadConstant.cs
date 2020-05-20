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

        public override string ToString() => $"{VariableDefs[0].Name} = 0x{Constant:X08}";
    }
}