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
    public class DeadCodeEliminator : AnalysisPass
    {
        public override void Run(IRContext context)
        {
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var block in context.Function.BasicBlocks)
                {
                    for (int i = 0; i < block.Instructions.Count; i++)
                    {
                        var instruction = block.Instructions[i];
                        if (!(instruction is IRAssignment assgn) || assgn.Source is IRCallExpression)
                            continue;

                        bool used = false;
                        foreach (var def in instruction.Defs)
                        {
                            if (def is IRStackVariable)
                            {
                                used = true;
                                break;
                            }

                            var uses = block.FindUses(i, def);
                            used |= uses.Length > 0;
                        }

                        if (!used)
                        {
                            block.Instructions.Remove(instruction);
                            i--;
                            changed = true;
                        }
                    }
                }
            }
        }
    }
}