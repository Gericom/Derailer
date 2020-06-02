using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.IR;
using LibDerailer.IR.Expressions;
using LibDerailer.IR.Instructions;

namespace LibDerailer.Analysis
{
    // Structurizes mini loops into standard case tables
    public class CondenseMiniSwitchesPass : AnalysisPass
    {
        bool IsInstructionSwitchHead(IRJump instr, ref IRVariable switchVar, bool initial)
        {
            if (instr is null)
                return false;

            if (initial)
                return instr.Condition is null;

            if (!(instr.Condition is IRComparisonExpression cmp))
                return false;

            if (cmp.Operator == IRComparisonOperator.Equal &&
                cmp.OperandA is IRVariable &&
                cmp.OperandB is IRConstant)
            {
                var opA = cmp.OperandA as IRVariable;
                if (switchVar is null)
                {
                    switchVar = opA;
                    return true;
                }

                return opA.Equals(switchVar);
            }

            return false;
        }

        // At this point, we know at minimum we have
        // cmp rA, x; beq; b;
        void HandleSwitchHead(IRContext context, IRVariable switchVar, int start, int size)
        {
            // If only one case exists, we probably don't care about it?
            if (size <= 2)
                return;

            File.WriteAllText(@"preswitch_basicblocks.txt", context.Function.BasicBlockGraphToDot());

            var sortedBlocks = context.Function.BasicBlocks.OrderBy(x => x.OrderIndex);

            var blocks = sortedBlocks.Skip(start).Take(size).ToArray();

            // All blocks only have one instruction
            var cases = from block in blocks.OrderBy(x => ((IRJump) x.Instructions[0]).Destination.OrderIndex)
                select block.Instructions[0];
            var head = blocks.First();

            // The new head, being the first block, naturally inherits the proper predecessors
            head.Successors.Clear();

            // Only the successors of the final block matter
            foreach (var succ in blocks.Last().Successors)
            {
                succ.Predecessors.Remove(blocks.Last());
                succ.Predecessors.Add(head);

                head.Successors.Add(succ);
            }

            // Collapse into switch table (modify the final, unconditional branch)
            head.SwitchVariable = switchVar;
            foreach (var switchCase in cases)
            {
                var jump = switchCase as IRJump;

                head.SwitchCases.Add(
                    ((jump.Condition as IRComparisonExpression)?.OperandB as IRConstant, jump.Destination));
                // This will be true for the initial case
                if (!head.Successors.Contains(jump.Destination))
                    head.Successors.Add(jump.Destination);
            }

            // Orphan unused nodes
            foreach (var block in blocks.Skip(1))
            {
                block.Predecessors.Clear();
                block.Successors.Clear();
                block.Instructions.Clear();
            }

            //find the follow node
            IRBasicBlock follow        = null;
            int          followInEdges = 0;
            for (int i = head.ReversePostOrderIndex + 1; i < context.Function.BasicBlocks.Count; i++)
            {
                var dominatedBlock = context.Function.BasicBlocks[i];
                if (dominatedBlock.ImmediateDominator != head)
                    continue;
                int followInEdgesNew = dominatedBlock.Predecessors.Count - dominatedBlock.BackEdgeCount;
                //if (followInEdgesNew >= followInEdges)
                {
                    follow        = dominatedBlock;
                    followInEdges = followInEdgesNew;
                }
            }

            head.SwitchFollow = follow;

            File.WriteAllText(@"switch_basicblocks.txt", context.Function.BasicBlockGraphToDot());
        }

        public override void Run(IRContext context)
        {
            int        switchSpan = 0;
            IRVariable switchVar  = null;

            var sortedBlocks = context.Function.BasicBlocks.OrderBy(x => x.OrderIndex).ToArray();

            for (int i = sortedBlocks.Length - 1; i >= 0; i--)
            {
                var block        = sortedBlocks[i];
                var instructions = block.Instructions;

                if (instructions.Count == 1 &&
                    IsInstructionSwitchHead(instructions[0] as IRJump, ref switchVar, switchSpan == 0))
                {
                    switchSpan++;
                    continue;
                }

                if (switchSpan > 0)
                    HandleSwitchHead(context, switchVar, i, switchSpan);

                switchSpan = 0;
            }

            if (switchSpan > 0)
                HandleSwitchHead(context, switchVar, 0, switchSpan);
        }
    }
}