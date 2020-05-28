using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gee.External.Capstone.Arm;
using LibDerailer.CodeGraph.Nodes;
using LibDerailer.IR;
using LibDerailer.IR.Instructions;

namespace LibDerailer.CodeGraph
{
    public class BasicBlock : IComparable<BasicBlock>
    {
        public uint Address { get; }
        public int Order { get; set; }

        public Instruction BlockConditionInstruction { get; set; }
        public ArmConditionCode BlockCondition { get; }

        public List<Instruction> Instructions { get; } = new List<Instruction>();

        public List<BasicBlock> Predecessors { get; } = new List<BasicBlock>();
        public List<BasicBlock> Successors   { get; } = new List<BasicBlock>();

        public Branch BlockBranch { get; set; }

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

        public IEnumerable<IRInstruction> GetIRInstructions(IRContext context) 
            => Instructions.SelectMany(i => i.GetIRInstructions(context, context.BasicBlockMapping[this]));

        public override string ToString() => $"0x{Address:X08}";
    }
}