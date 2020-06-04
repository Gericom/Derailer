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
    public class LongShiftFixupPass : AnalysisPass
    {
        public override void Run(IRContext context)
        {
            var longRightShiftVar = new IRRegisterVariable(IRPrimitive.U64, "mergeVar");
            var shiftAmountA = new IRRegisterVariable(IRPrimitive.S32, "a");
            var shiftAmountB = new IRRegisterVariable(IRPrimitive.S32, "b");
            var shiftAmountNew = new IRRegisterVariable(IRPrimitive.U64, "c");
            var longRightShiftTemplate =
                longRightShiftVar.Cast(IRPrimitive.U32).ShiftRightLogical(shiftAmountA) |
                longRightShiftVar.ShiftRightLogical(32).Cast(IRPrimitive.U32).ShiftLeft(shiftAmountB);
            var longRightShiftSubst = longRightShiftVar.ShiftRightLogical(shiftAmountNew).Cast(IRPrimitive.U32);
            foreach (var basicBlock in context.Function.BasicBlocks)
                foreach (var instruction in basicBlock.Instructions)
                    instruction.Substitute(longRightShiftTemplate, longRightShiftSubst, mapping =>
                    {
                        var shiftA = mapping[shiftAmountA] as IRConstant<int>;
                        var shiftB = mapping[shiftAmountB] as IRConstant<int>;
                        if (shiftA is null || shiftB is null)
                            return false;
                        if (shiftA.Value <= 0 || shiftA.Value >= 32)
                            return false;
                        if (shiftB.Value != 32 - shiftA.Value)
                            return false;
                        mapping[shiftAmountNew] = (ulong) shiftA.Value;
                        return true;
                    });
        }
    }
}
