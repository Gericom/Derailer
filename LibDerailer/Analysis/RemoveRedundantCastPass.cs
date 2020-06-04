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
    public class RemoveRedundantCastPass : AnalysisPass
    {
        public override void Run(IRContext context)
        {
            var s32Var = new IRRegisterVariable(IRPrimitive.S32, "x");
            foreach (var basicBlock in context.Function.BasicBlocks)
                foreach (var instruction in basicBlock.Instructions)
                    instruction.Substitute(s32Var.Cast(IRPrimitive.U32).Cast(IRPrimitive.S32), s32Var);

            var u32Var = new IRRegisterVariable(IRPrimitive.U32, "x");
            foreach (var basicBlock in context.Function.BasicBlocks)
                foreach (var instruction in basicBlock.Instructions)
                    instruction.Substitute(u32Var.Cast(IRPrimitive.S32).Cast(IRPrimitive.U32), u32Var);
        }
    }
}