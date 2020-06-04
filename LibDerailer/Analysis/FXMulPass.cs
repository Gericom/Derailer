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
    public class FXMulPass : AnalysisPass
    {
        public override void Run(IRContext context)
        {
            var fxMulVarA = new IRRegisterVariable(IRPrimitive.S32, "a");
            var fxMulVarB = new IRRegisterVariable(IRPrimitive.S32, "b");
            var fxMulTemplate =
                ((fxMulVarA.Cast(IRPrimitive.S64) * fxMulVarB.Cast(IRPrimitive.S64)).Cast(IRPrimitive.U64) +
                 new IRConversionExpression(IRPrimitive.U64, 0x800u))
                .ShiftRightLogical(12ul).Cast(IRPrimitive.U32);
            var fxMulSubst = new IRCallExpression(IRPrimitive.S32, "FX_Mul", fxMulVarA, fxMulVarB).Cast(IRPrimitive.U32);
            foreach (var basicBlock in context.Function.BasicBlocks)
                foreach (var instruction in basicBlock.Instructions)
                    instruction.Substitute(fxMulTemplate, fxMulSubst);
        }
    }
}