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
        void HandleSwitchHead(IRContext context, IRVariable switchVar, int start, int size, bool defaultIsEpilogue)
        {
            // If only one case exists, we probably don't care about it?
            if (size <= 1)
                return;

            if (defaultIsEpilogue)
                size--;

            File.WriteAllText(@"preswitch_basicblocks.txt", context.Function.BasicBlockGraphToDot());

            var sortedBlocks = context.Function.BasicBlocks.OrderBy(x => x.OrderIndex);

            var blocks2 = sortedBlocks.Skip(start).Take(size);
            // if (defaultIsEpilogue)
                // blocks2 = blocks2.Append(context.Function.Epilogue);
            var blocks = blocks2.ToArray();

            // All blocks only have one instruction
            var cases = from block in blocks.OrderBy(x => x.BlockJump.Destination.OrderIndex)
                select block.BlockJump;
            var head = blocks.First();

            // The new head, being the first block, naturally inherits the proper predecessors
            head.Successors.Clear();

            // Only the successors of the final block matter
            if(defaultIsEpilogue)
            {
                context.Function.Epilogue.Predecessors.Add(head);

                head.Successors.Add(context.Function.Epilogue);
            }
            else
            {
                foreach (var succ in blocks.Last().Successors)
                {
                    succ.Predecessors.Remove(blocks.Last());
                    succ.Predecessors.Add(head);

                    head.Successors.Add(succ);
                }
            }

            // Collapse into switch table (modify the final, unconditional branch)
            head.SwitchVariable = switchVar;
            foreach (var switchCase in cases)
            {
                var jump = switchCase;

                head.SwitchCases.Add(
                    ((jump.Condition as IRComparisonExpression)?.OperandB as IRConstant, jump.Destination));
                if (!jump.Destination.Predecessors.Contains(head))
                    jump.Destination.Predecessors.Add(head);
                // This will be true for the initial case
                if (!head.Successors.Contains(jump.Destination))
                    head.Successors.Add(jump.Destination);
            }

            // Orphan unused nodes
            foreach (var block in blocks.Skip(1))
            {
                foreach (var predecessor in block.Predecessors)
                    predecessor.Successors.Remove(block);
                block.Predecessors.Clear();
                foreach (var successor in block.Successors)
                    successor.Predecessors.Remove(block);
                block.Successors.Clear();
                block.Instructions.Clear();
                block.BlockJump = null;
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
            int        switchSpan        = 0;
            bool       defaultIsEpilogue = false;
            IRVariable switchVar         = null;

            var sortedBlocks = context.Function.BasicBlocks.OrderBy(x => x.OrderIndex).ToArray();

            for (int i = sortedBlocks.Length - 1; i >= 0; i--)
            {
                var block        = sortedBlocks[i];
                var instructions = block.Instructions;

                if (switchSpan == 0 &&
                    instructions.Count == 1 &&
                    block.Successors.Count == 2 &&
                    block.Successors.First(s => s != block.BlockJump.Destination) == context.Function.Epilogue &&
                    IsInstructionSwitchHead(block.BlockJump, ref switchVar, false))
                {
                    switchSpan        += 2;
                    defaultIsEpilogue =  true;
                    continue;
                }
                else if (instructions.Count == 1 &&
                         IsInstructionSwitchHead(block.BlockJump, ref switchVar, switchSpan == 0))
                {
                    switchSpan++;
                    continue;
                }
                else if (switchSpan > 0 && instructions.Count > 1 && block.Successors.Count == 2 &&
                         IsInstructionSwitchHead(block.BlockJump, ref switchVar, false))
                {
                    switchSpan++;
                }

                if (switchSpan > 0)
                    HandleSwitchHead(context, switchVar, i, switchSpan, defaultIsEpilogue);

                switchSpan        = 0;
                defaultIsEpilogue = false;
            }

            if (switchSpan > 0)
                HandleSwitchHead(context, switchVar, 0, switchSpan, defaultIsEpilogue);
        }
    }
}