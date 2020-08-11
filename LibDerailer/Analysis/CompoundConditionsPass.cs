using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.IR;
using LibDerailer.IR.Expressions;
using LibDerailer.IR.Instructions;

namespace LibDerailer.Analysis
{
    public class CompoundConditionsPass : AnalysisPass
    {
        public override void Run(IRContext context)
        {
            bool change = true;
            while (change)
            {
                change = false;
                for (int i = 0; i < context.Function.BasicBlocks.Count; i++)
                {
                    var block = context.Function.BasicBlocks[i];
                    if (block.Successors.Count != 2)
                        continue;
                    var elseBlock = block.BlockJump.Destination;
                    var ifBlock   = block.Successors.First(s => s != elseBlock);
                    // var elseBlock = block.Successors[0];
                    // var ifBlock = block.Successors[1];
                    // if(ifBlock.OrderIndex > elseBlock.OrderIndex)
                    // {
                    //     var tmp = ifBlock;
                    //     ifBlock = elseBlock;
                    //     elseBlock = tmp;
                    // }
                    if (elseBlock.Successors.Count == 0 && elseBlock != context.Function.Epilogue)
                        continue;

                    //change = true;
                    if (ifBlock.Successors.Count == 2 && ifBlock.Predecessors.Count == 1 &&
                        ifBlock.BlockJump.Destination == elseBlock)
                    {
                        if (ifBlock.Instructions.Count != 1)
                        {
                            var assignments = ifBlock.Instructions.OfType<IRAssignment>()
                                .Where(assgn => assgn.Destination is IRVariable).ToArray();
                            if (assignments.Length == ifBlock.Instructions.Count - 1)
                            {
                                foreach (var instruction in assignments)
                                {
                                    var variable  = (IRVariable) instruction.Destination;
                                    var hoistUses = ifBlock.FindUses(instruction, variable);
                                    foreach (var use in hoistUses)
                                        use.SubstituteUse(variable, instruction.Source);
                                    ifBlock.Instructions.Remove(instruction);
                                }
                            }
                            else
                                continue;
                        }

                        block.BlockJump.Condition |= ifBlock.BlockJump.Condition;

                        int ifIdx = block.Successors.IndexOf(elseBlock);
                        var next  = ifBlock.Successors.First(s => s != elseBlock);
                        block.Successors[1 - ifIdx] = next;
                        next.Predecessors.Remove(ifBlock);
                        next.Predecessors.Add(block);
                        elseBlock.Predecessors.Remove(ifBlock);
                        ifBlock.Successors.Clear();
                        ifBlock.Instructions.Clear();
                        i--;
                        change = true;
                    }
                    else if (ifBlock.Successors.Count == 2 && ifBlock.Predecessors.Count == 1 &&
                             ifBlock.Successors.First(s => s != ifBlock.BlockJump.Destination) == elseBlock)
                    {
                        if (ifBlock.Instructions.Count != 1)
                        {
                            var assignments = ifBlock.Instructions.OfType<IRAssignment>()
                                .Where(assgn => assgn.Destination is IRVariable).ToArray();
                            if (assignments.Length == ifBlock.Instructions.Count - 1)
                            {
                                foreach (var instruction in assignments)
                                {
                                    var variable  = (IRVariable) instruction.Destination;
                                    var hoistUses = ifBlock.FindUses(instruction, variable);
                                    foreach (var use in hoistUses)
                                        use.SubstituteUse(variable, instruction.Source);
                                    ifBlock.Instructions.Remove(instruction);
                                }
                            }
                            else
                                continue;
                        }

                        block.BlockJump.Condition |= ifBlock.BlockJump.Condition.InverseCondition();

                        int ifIdx = block.Successors.IndexOf(elseBlock);
                        var next  = ifBlock.Successors.First(s => s != elseBlock);
                        block.Successors[1 - ifIdx] = next;
                        next.Predecessors.Remove(ifBlock);
                        next.Predecessors.Add(block);
                        elseBlock.Predecessors.Remove(ifBlock);
                        ifBlock.Successors.Clear();
                        ifBlock.Instructions.Clear();
                        i--;
                        change = true;
                    }
                    else if (elseBlock.Successors.Count == 2 && elseBlock.Predecessors.Count == 1 &&
                             elseBlock.Successors.First(s => s != elseBlock.BlockJump.Destination) == ifBlock)
                    {
                        if (elseBlock.Instructions.Count != 1)
                        {
                            var assignments = elseBlock.Instructions.OfType<IRAssignment>()
                                .Where(assgn => assgn.Destination is IRVariable).ToArray();
                            if (assignments.Length == elseBlock.Instructions.Count - 1)
                            {
                                foreach (var instruction in assignments)
                                {
                                    var variable  = (IRVariable) instruction.Destination;
                                    var hoistUses = elseBlock.FindUses(instruction, variable);
                                    foreach (var use in hoistUses)
                                        use.SubstituteUse(variable, instruction.Source);
                                    elseBlock.Instructions.Remove(instruction);
                                }
                            }
                            else
                                continue;
                        }

                        block.BlockJump.Condition &= elseBlock.BlockJump.Condition;

                        int ifIdx = block.Successors.IndexOf(elseBlock);
                        var next  = elseBlock.Successors.First(s => s != ifBlock);
                        if (block.BlockJump.Destination == block.Successors[ifIdx])
                            block.BlockJump.Destination = next;
                        block.Successors[ifIdx] = next;
                        next.Predecessors.Remove(elseBlock);
                        next.Predecessors.Add(block);
                        ifBlock.Predecessors.Remove(elseBlock);
                        elseBlock.Successors.Clear();
                        elseBlock.Instructions.Clear();
                        i--;
                        change = true;
                    }
                    else if (elseBlock.Successors.Count == 2 && elseBlock.Predecessors.Count == 1 &&
                             elseBlock.BlockJump.Destination == ifBlock)
                    {
                        if (elseBlock.Instructions.Count != 1)
                            continue;

                        block.BlockJump.Condition &= elseBlock.BlockJump.Condition.InverseCondition();

                        int ifIdx = block.Successors.IndexOf(elseBlock);
                        var next  = elseBlock.Successors.First(s => s != ifBlock);
                        if (block.BlockJump.Destination == block.Successors[ifIdx])
                            block.BlockJump.Destination = next;
                        block.Successors[ifIdx] = next;
                        next.Predecessors.Remove(elseBlock);
                        next.Predecessors.Add(block);
                        ifBlock.Predecessors.Remove(elseBlock);
                        elseBlock.Successors.Clear();
                        elseBlock.Instructions.Clear();
                        i--;
                        change = true;
                    }
                }
            }
        }
    }
}