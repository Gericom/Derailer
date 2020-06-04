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
    public class ShiftToCastPass : AnalysisPass
    {
        public override void Run(IRContext context)
        {
            var x = new IRRegisterVariable(IRPrimitive.U32, "x");
            foreach (var basicBlock in context.Function.BasicBlocks)
                foreach (var instruction in basicBlock.Instructions)
                    instruction.Substitute(x.ShiftLeft(16u).ShiftRightLogical(16u),
                        x.Cast(IRPrimitive.U16).Cast(IRPrimitive.U32));

            foreach (var basicBlock in context.Function.BasicBlocks)
                foreach (var instruction in basicBlock.Instructions)
                    instruction.Substitute(x.ShiftLeft(16u).ShiftRightArithmetic(16u),
                        x.Cast(IRPrimitive.S16).Cast(IRPrimitive.U32));

            foreach (var basicBlock in context.Function.BasicBlocks)
                foreach (var instruction in basicBlock.Instructions)
                    instruction.Substitute(x.ShiftLeft(24u).ShiftRightLogical(24u),
                        x.Cast(IRPrimitive.U8).Cast(IRPrimitive.U32));

            foreach (var basicBlock in context.Function.BasicBlocks)
                foreach (var instruction in basicBlock.Instructions)
                    instruction.Substitute(x.ShiftLeft(24u).ShiftRightArithmetic(24u),
                        x.Cast(IRPrimitive.S8).Cast(IRPrimitive.U32));
        }
    }
}
