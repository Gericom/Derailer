using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gee.External.Capstone.Arm;
using LibDerailer.CodeGraph.Nodes;

namespace LibDerailer.CodeGraph
{
    public class BasicBlock : IGraphNode<BasicBlock>, IComparable<BasicBlock>
    {
        public bool IsLatchNode { get; set; }

        public BasicBlock LatchNode { get; set; }

        public LoopType LoopType { get; set; }

        public BasicBlock LoopHead { get; set; }
        public BasicBlock LoopFollow { get; set; }

        public BasicBlock IfFollow { get; set; }

        public uint Address { get; }

        public Instruction BlockConditionInstruction { get; set; }
        public ArmConditionCode BlockCondition { get; }

        public List<Instruction> Instructions { get; } = new List<Instruction>();

        public List<BasicBlock> Predecessors { get; } = new List<BasicBlock>();
        public List<BasicBlock> Successors   { get; } = new List<BasicBlock>();

        public Branch BlockBranch { get; set; }
        
        public int PreOrderIndex { get; set; }
        public int ReversePostOrderIndex { get; set; }
        public BasicBlock ImmediateDominator { get; set; }

        public int BackEdgeCount { get; set; }

        public BasicBlock(uint address, ArmConditionCode blockCondition = ArmConditionCode.Invalid)
        {
            Address        = address;
            BlockCondition = blockCondition;
        }

        public int CompareTo(BasicBlock other)
        {
            if (ReferenceEquals(this, other))
                return 0;
            if (ReferenceEquals(null, other))
                return 1;
            return Address.CompareTo(other.Address);
        }

        public Instruction GetLastDef(Variable variable)
        {
            return Instructions.FindLast(i => i.VariableDefs.Contains(variable));
        }

        public void MergeAppend(BasicBlock b)
        {
            var prevBlock = this;
            var nextBlock = b;
            prevBlock.Instructions.AddRange(nextBlock.Instructions);
            prevBlock.Successors.Remove(nextBlock);
            foreach (var successor in nextBlock.Successors)
            {
                if (!prevBlock.Successors.Contains(successor))
                    prevBlock.Successors.Add(successor);
                successor.Predecessors.Remove(nextBlock);
                if (!successor.Predecessors.Contains(prevBlock))
                    successor.Predecessors.Add(prevBlock);
            }

            BlockBranch = b.BlockBranch;
        }

        public override string ToString() => $"0x{Address:X08}";

        public static IntervalNode[][] GetIntervalSequence(IEnumerable<BasicBlock> blocks, BasicBlock root)
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