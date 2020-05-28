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
    public class ExpressionPropagator : AnalysisPass
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
                        var          instruction = block.Instructions[i];
                        IRVariable   v           = null;
                        IRExpression irExpr      = null;
                        IRInstruction instr = null;
                        foreach (var use in instruction.Uses)
                        {
                            if (use is IRStackVariable)
                                continue;
                            var defs = block.FindDefs(i, use);
                            if (defs.Length != 1)
                                continue;
                            if (defs[0] is IRAssignment assgn && !(assgn.Source is IRCallExpression))
                            {
                                if (assgn.ParentBlock.FindUses(assgn, use).Length != 1)
                                    continue;
                                v      = use;
                                irExpr = assgn.Source;
                                instr = assgn;
                                break;
                            }
                        }

                        if (!(irExpr is null))
                        {
                            //check that all defs of uses are equal
                            bool ok = true;
                            foreach (var exprVar in irExpr.GetAllVariables())
                            {
                                var defsOriginal = instr.ParentBlock.FindDefs(instr, exprVar);
                                var defsNew = block.FindDefs(instruction, exprVar);
                                if(!defsOriginal.ToHashSet().SetEquals(defsNew))
                                {
                                    ok = false;
                                    break;
                                }
                            }

                            if (ok)
                            {
                                instruction.SubstituteUse(v, irExpr);
                                changed = true;
                            }
                        }

                        if (changed)
                            break;
                    }
                    if (changed)
                        break;
                }
                new DeadCodeEliminator().Run(context);
            }
        }
    }
}