using System.Collections.Generic;

namespace LibDerailer.CodeGraph.Nodes
{
    public abstract class Instruction
    {
        public List<Variable> VariableUses { get; } = new List<Variable>();
        public List<Variable> VariableDefs { get; } = new List<Variable>();

        public Dictionary<Variable, Instruction[]> VariableUseLocs { get; } = new Dictionary<Variable, Instruction[]>();
        public Dictionary<Variable, Instruction[]> VariableDefLocs { get; } = new Dictionary<Variable, Instruction[]>();
    }
}