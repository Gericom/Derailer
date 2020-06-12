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

            var u8Var = new IRRegisterVariable(IRPrimitive.U8, "x");
            foreach (var basicBlock in context.Function.BasicBlocks)
                foreach (var instruction in basicBlock.Instructions)
                    instruction.Substitute(u8Var.Cast(IRPrimitive.U32).Cast(IRPrimitive.U8), u8Var);

            foreach (var basicBlock in context.Function.BasicBlocks)
                foreach (var instruction in basicBlock.Instructions)
                    instruction.Substitute(u32Var.Cast(IRPrimitive.U8).Cast(IRPrimitive.U32), u32Var,
                        map => map[u32Var] is IRStructFieldExpression sf && sf.StructField.BitCount == 8);

            var anyExpr = new IRRegisterVariable(new IRMatchType(a => true), "x");
            foreach (var basicBlock in context.Function.BasicBlocks)
            {
                foreach (var instruction in basicBlock.Instructions)
                {
                    instruction.Substitute(anyExpr, anyExpr, map =>
                    {
                        if (map[anyExpr] is IRConversionExpression conv && conv.Type == conv.Operand.Type)
                        {
                            map[anyExpr] = conv.Operand;
                            return true;
                        }

                        return false;
                    });
                }
            }

            var pointerExpr = new IRRegisterVariable(new IRMatchType(a => a is IRPointer), "x");
            foreach (var basicBlock in context.Function.BasicBlocks)
                foreach (var instruction in basicBlock.Instructions)
                    instruction.Substitute(pointerExpr, pointerExpr, map =>
                    {
                        if (map[pointerExpr] is IRConversionExpression conv && conv.Operand is IRConversionExpression conv2 &&
                            conv2.Type == IRPrimitive.U32 && conv2.Operand.Type == conv.Type)
                        {
                            map[pointerExpr] = conv2.Operand;
                            return true;
                        }

                        return false;
                    });
        }
    }
}