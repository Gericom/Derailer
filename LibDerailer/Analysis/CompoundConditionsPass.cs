using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.IR;

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
                    var ifBlock = block.Successors.First(s => s != elseBlock);
                    if (elseBlock.Successors.Count == 0)
                        continue;

                    change = true;
                    if (ifBlock.Successors.Count == 2 && ifBlock.Predecessors.Count == 1 &&
                        ifBlock.Instructions.Count == 1 && ifBlock.BlockJump.Destination == elseBlock)
                    {
                        block.BlockJump.Condition |= ifBlock.BlockJump.Condition;

                        int ifIdx = block.Successors.IndexOf(elseBlock);
                        var next = ifBlock.Successors.First(s => s != elseBlock);
                        block.Successors[1 - ifIdx] = next;
                        next.Predecessors.Remove(ifBlock);
                        next.Predecessors.Add(block);
                        elseBlock.Predecessors.Remove(ifBlock);
                        ifBlock.Successors.Clear();
                        ifBlock.Instructions.Clear();
                        i--;
                    }
                    else if (ifBlock.Successors.Count == 2 && ifBlock.Predecessors.Count == 1 &&
                             ifBlock.Instructions.Count == 1 &&
                             ifBlock.Successors.First(s => s != ifBlock.BlockJump.Destination) == elseBlock)
                    {
                        block.BlockJump.Condition |= ifBlock.BlockJump.Condition.InverseCondition();

                        int ifIdx = block.Successors.IndexOf(elseBlock);
                        var next = ifBlock.Successors.First(s => s != elseBlock);
                        block.Successors[1 - ifIdx] = next;
                        next.Predecessors.Remove(ifBlock);
                        next.Predecessors.Add(block);
                        elseBlock.Predecessors.Remove(ifBlock);
                        ifBlock.Successors.Clear();
                        ifBlock.Instructions.Clear();
                        i--;
                    }
                    else if (elseBlock.Successors.Count == 2 && elseBlock.Predecessors.Count == 1 &&
                             elseBlock.Instructions.Count == 1 &&
                             elseBlock.Successors.First(s => s != elseBlock.BlockJump.Destination) == ifBlock)
                    {
                        block.BlockJump.Condition &= elseBlock.BlockJump.Condition;

                        int ifIdx = block.Successors.IndexOf(elseBlock);
                        var next = elseBlock.Successors.First(s => s != ifBlock);
                        if (block.BlockJump.Destination == block.Successors[ifIdx])
                            block.BlockJump.Destination = next;
                        block.Successors[ifIdx] = next;
                        next.Predecessors.Remove(elseBlock);
                        next.Predecessors.Add(block);
                        ifBlock.Predecessors.Remove(elseBlock);
                        elseBlock.Successors.Clear();
                        elseBlock.Instructions.Clear();
                        i--;
                    }
                    else if (elseBlock.Successors.Count == 2 && elseBlock.Predecessors.Count == 1 &&
                             elseBlock.Instructions.Count == 1 && elseBlock.BlockJump.Destination == ifBlock)
                    {
                        throw new NotImplementedException();
                    }
                    else
                        change = false;
                }
            }
        }
    }
}
