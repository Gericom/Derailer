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
    public class ConstDivisionPass : AnalysisPass
    {
        public override void Run(IRContext context)
        {
            var x        = new IRRegisterVariable(IRPrimitive.U32, "x");
            var magicVal = new IRRegisterVariable(IRPrimitive.U32, "magicVal");
            var shiftVal = new IRRegisterVariable(IRPrimitive.S32, "shiftVal");
            var divisor  = new IRRegisterVariable(IRPrimitive.S32, "divisor");
            var subst    = (x.Cast(IRPrimitive.S32) / divisor).Cast(IRPrimitive.U32);

            //Signed, division by two
            foreach (var basicBlock in context.Function.BasicBlocks)
                foreach (var instruction in basicBlock.Instructions)
                    instruction.Substitute((x + x.ShiftRightLogical(31)).ShiftRightArithmetic(1),
                        (x.Cast(IRPrimitive.S32) / 2).Cast(IRPrimitive.U32));

            //Signed, no shift, positive constant
            var template = (magicVal.Cast(IRPrimitive.S32).Cast(IRPrimitive.S64) *
                            x.Cast(IRPrimitive.S32).Cast(IRPrimitive.S64)).ShiftRightLogical(32)
                           .Cast(IRPrimitive.U32) +
                           x.ShiftRightLogical(31);
            foreach (var basicBlock in context.Function.BasicBlocks)
            {
                foreach (var instruction in basicBlock.Instructions)
                {
                    instruction.Substitute(template, subst, mapping =>
                    {
                        var magic = mapping[magicVal] as IRConstant<uint>;

                        if (magic is null)
                            return false;

                        int magicSigned = (int) magic.Value;

                        float divisorVal = (float) (1ul << 32) / magicSigned;

                        if (divisorVal != (float) Math.Round(divisorVal))
                            return false;

                        mapping[divisor] = (int) divisorVal;
                        return true;
                    });
                }
            }

            //Signed, shift, positive constant
            template = x.ShiftRightLogical(31) +
                       (magicVal.Cast(IRPrimitive.S32).Cast(IRPrimitive.S64) *
                        x.Cast(IRPrimitive.S32).Cast(IRPrimitive.S64)).ShiftRightLogical(32)
                       .Cast(IRPrimitive.U32).ShiftRightArithmetic(shiftVal);
            foreach (var basicBlock in context.Function.BasicBlocks)
            {
                foreach (var instruction in basicBlock.Instructions)
                {
                    instruction.Substitute(template, subst, mapping =>
                    {
                        var magic = mapping[magicVal] as IRConstant<uint>;
                        var shift = mapping[shiftVal] as IRConstant<int>;

                        if (magic is null || shift is null)
                            return false;

                        int magicSigned = (int) magic.Value;

                        float divisorVal = (float) (1ul << 32 << shift.Value) / magicSigned;

                        if (divisorVal != (float) Math.Round(divisorVal))
                            return false;

                        mapping[divisor] = (int) divisorVal;
                        return true;
                    });
                }
            }

            //Signed, shift, negative constant
            template = x.ShiftRightLogical(31) +
                       (x + (magicVal.Cast(IRPrimitive.S32).Cast(IRPrimitive.S64) *
                             x.Cast(IRPrimitive.S32).Cast(IRPrimitive.S64)).ShiftRightLogical(32)
                           .Cast(IRPrimitive.U32)).ShiftRightArithmetic(shiftVal);
            foreach (var basicBlock in context.Function.BasicBlocks)
            {
                foreach (var instruction in basicBlock.Instructions)
                {
                    instruction.Substitute(template, subst, mapping =>
                    {
                        var magic = mapping[magicVal] as IRConstant<uint>;
                        var shift = mapping[shiftVal] as IRConstant<int>;

                        if (magic is null || shift is null)
                            return false;

                        int magicSigned = (int) magic.Value;

                        float divisorVal = (float) (1ul << 32 << shift.Value) / (magicSigned + (1L << 32));

                        if (divisorVal != (float) Math.Round(divisorVal))
                            return false;

                        mapping[divisor] = (int) divisorVal;
                        return true;
                    });
                }
            }
        }
    }
}