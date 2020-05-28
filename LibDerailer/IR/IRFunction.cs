using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gee.External.Capstone.Arm;
using LibDerailer.CodeGraph;
using LibDerailer.CodeGraph.Nodes;
using LibDerailer.IR.Instructions;

namespace LibDerailer.IR
{
    public class IRFunction
    {
        public List<IRBasicBlock> BasicBlocks { get; } = new List<IRBasicBlock>();
        public IRBasicBlock       Epilogue    { get; set; }

        public string BasicBlockGraphToDot()
        {
            int id        = 0;
            var uniqueIds = new Dictionary<Instruction, int>();
            var sb        = new StringBuilder();
            sb.AppendLine("digraph func {");
            foreach (var block in BasicBlocks)
            {
                foreach (var successor in block.Successors)
                {
                    sb.AppendLine($"\"{block.ReversePostOrderIndex}\" -> \"{successor.ReversePostOrderIndex}\" [label=\"{(block.BlockJump?.Destination == successor ? "jump" : "")}\"];");
                }
            }

            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}