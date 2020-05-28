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
    public class ConstantPropagator : AnalysisPass
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
                        IRVariable v       = null;
                        IRConstant irConst = null;
                        foreach (var use in instruction.Uses)
                        {
                            var defs = block.FindDefs(i, use);
                            if (defs.Length != 1)
                                continue;
                            if (defs[0] is IRAssignment assgn && assgn.Source is IRConstant irc)
                            {
                                irConst = irc;
                                v       = use;
                                break;
                            }
                        }

                        if (!(irConst is null))
                        {
                            instruction.SubstituteUse(v, irConst);
                            changed = true;
                        }
                    }
                }
            }
        }
    }
}
