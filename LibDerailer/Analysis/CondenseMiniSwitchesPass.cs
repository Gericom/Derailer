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
        bool IsInstructionSwitchHead(IRJump instr, ref IRRegisterVariable sw_reg, bool initial)
        {
            if (instr is null)
                return false;

            if (initial)
                return instr.Condition is null;

            var cmp = instr.Condition as IRComparisonExpression;
            if (cmp is null)
                return false;

            if (cmp.Operator == IRComparisonOperator.Equal &&
                cmp.OperandA is IRRegisterVariable &&
                cmp.OperandB is IRConstant)
            {
                var op_a = cmp.OperandA as IRRegisterVariable;
                if (sw_reg is null)
                {
                    sw_reg = op_a;
                    return true;
                }
                else
                {
                    return op_a.Equals(sw_reg);
                }
            }
            else
            {
                return false;
            }
        }

        // At this point, we know at minimum we have
        // cmp rA, x; beq; b;
        void HandleSwitchHead(IRContext context, IRRegisterVariable rv, int start, int size)
        {
            // If only one case exists, we probably don't care about it?
            if (size <= 2) return;

            File.WriteAllText(@"preswitch_basicblocks.txt", context.Function.BasicBlockGraphToDot());

            var blocks_sorted = context.Function.BasicBlocks.OrderBy(x => x.OrderIndex);

            var blocks = blocks_sorted.Skip(start).Take(size);         
            
            // All blocks only have one instruction
            var cases = from block in blocks.OrderBy(x => (x.Instructions[0] as IRJump).Destination.OrderIndex)
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
            head.CaseRegisterVariable = rv;
            foreach (var case_ in cases)
            {
                var jump = case_ as IRJump;

                head.CaseSuccessors.Add((jump.Condition is null ? null : (jump.Condition as IRComparisonExpression).OperandB as IRConstant,
                    jump.Destination));
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

            File.WriteAllText(@"switch_basicblocks.txt", context.Function.BasicBlockGraphToDot());
        }
        public override void Run(IRContext context)
        {
            int sw_span = 0;
            IRRegisterVariable sw_reg = null;

            var blocks_sorted = context.Function.BasicBlocks.OrderBy(x => x.OrderIndex).ToArray();

            for (int i = blocks_sorted.Length - 1; i >= 0; --i)
            {
                var block = blocks_sorted[i];
                var instructions = block.Instructions;

                if (instructions.Count == 1 && IsInstructionSwitchHead(instructions[0] as IRJump, ref sw_reg, sw_span == 0))
                {
                    ++sw_span;
                    continue;
                }

                if (sw_span > 0)
                    HandleSwitchHead(context, sw_reg, i, sw_span);

                sw_span = 0;
            }

            if (sw_span > 0)
                HandleSwitchHead(context, sw_reg, 0, sw_span);
        }
    }
}
