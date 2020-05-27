using System.Collections.Generic;
using System.Linq;
using LibDerailer.CodeGraph;
using LibDerailer.IR.Instructions;

namespace LibDerailer.IR
{
    public class IRBasicBlock
    {
        public List<IRBasicBlock> Predecessors { get; } = new List<IRBasicBlock>();
        public List<IRBasicBlock> Successors   { get; } = new List<IRBasicBlock>();

        public List<IRInstruction> Instructions { get; } = new List<IRInstruction>();

        public bool IsLatchNode { get; set; }

        public IRBasicBlock LatchNode { get; set; }

        public LoopType LoopType { get; set; }

        public IRBasicBlock LoopHead     { get; set; }
        public IRBasicBlock LoopFollow   { get; set; }
        public IRBasicBlock ForLoopInitialIf { get; set; }

        public IRBasicBlock IfFollow { get; set; }

        public IRJump BlockBranch { get; set; }

        public int          PreOrderIndex         { get; set; }
        public int          ReversePostOrderIndex { get; set; }
        public IRBasicBlock ImmediateDominator    { get; set; }

        public int BackEdgeCount { get; set; }

        public static IntervalNode[][] GetIntervalSequence(IEnumerable<IRBasicBlock> blocks, IRBasicBlock root)
        {
            //wrap original graph in IntervalNodes
            var g1 = blocks.Select(b => new IntervalNode(b)).ToArray();
            foreach (var node in g1)
            {
                node.Predecessors.AddRange(node.Block.Predecessors.Select(s => g1.First(g => g.Block == s)));
                node.Successors.AddRange(node.Block.Successors.Select(s => g1.First(g => g.Block == s)));
            }

            return IntervalNode.GetIntervalSequence(g1, g1.First(b => b.Block == root));
        }
    }
}