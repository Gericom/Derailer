using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.IR;
using LibDerailer.IR.Expressions;
using LibDerailer.IR.Types;

namespace LibDerailer.Analysis
{
    public class MergeS64SubExpressionsPass : AnalysisPass
    {
        public override void Run(IRContext context)
        {
            var s64ExprMergeVar = new IRRegisterVariable(IRPrimitive.S64, "mergeVar");
            var s64ExprMergeTemplate =
                s64ExprMergeVar.Cast(IRPrimitive.U32).Cast(IRPrimitive.U64) |
                s64ExprMergeVar.ShiftRightLogical(32).Cast(IRPrimitive.U32).Cast(IRPrimitive.U64).ShiftLeft(32);
            var s64ExprMergeSubst = s64ExprMergeVar.Cast(IRPrimitive.U64);
            foreach (var basicBlock in context.Function.BasicBlocks)
                foreach (var instruction in basicBlock.Instructions)
                    instruction.Substitute(s64ExprMergeTemplate, s64ExprMergeSubst);
        }
    }
}
