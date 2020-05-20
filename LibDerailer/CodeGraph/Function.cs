using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.CodeGraph.Nodes;

namespace LibDerailer.CodeGraph
{
    public class Function
    {
        public uint   Address { get; }
        public string Name    { get; }

        public List<BasicBlock> BasicBlocks    { get; } = new List<BasicBlock>();

        public int StackOffset { get; }
        public List<Variable>   StackVariables { get; } = new List<Variable>();

        public Function(uint address, int stackOffset = 0)
        {
            Address = address;
            StackOffset = stackOffset;
        }

        public string DefUseGraphToDot()
        {
            int id        = 0;
            var uniqueIds = new Dictionary<Instruction, int>();
            var sb        = new StringBuilder();
            sb.AppendLine("digraph func {");
            foreach (var block in BasicBlocks)
            {
                foreach (var inst in block.Instructions)
                {
                    if (!uniqueIds.ContainsKey(inst))
                        uniqueIds[inst] = id++;
                    foreach (var use in inst.VariableUses)
                    {
                        foreach (var useLoc in inst.VariableUseLocs[use])
                        {
                            if (!uniqueIds.ContainsKey(useLoc))
                                uniqueIds[useLoc] = id++;
                            sb.AppendLine(
                                $"\"{uniqueIds[useLoc]}: {useLoc}\" -> \"{uniqueIds[inst]}: {inst}\" [label={use.Name}];");
                        }
                    }
                }
            }

            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}