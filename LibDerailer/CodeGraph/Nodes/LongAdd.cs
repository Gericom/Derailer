using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gee.External.Capstone.Arm;
using LibDerailer.IR;
using LibDerailer.IR.Expressions;
using LibDerailer.IR.Instructions;
using LibDerailer.IR.Types;

namespace LibDerailer.CodeGraph.Nodes
{
    public class LongAdd : Instruction
    {
        public LongAdd(Variable dstLo, Variable dstHi, Variable opALo, Variable opAHi, Operand opBLo, Operand opBHi)
            : base(ArmConditionCode.ARM_CC_AL)
        {
            Operands.Add((true, dstLo));
            Operands.Add((true, dstHi));
            Operands.Add((false, opALo));
            Operands.Add((false, opAHi));
            Operands.Add((false, opBLo));
            Operands.Add((false, opBHi));
            VariableUses.Add(opALo);
            VariableUses.Add(opAHi);
            if (opBLo is Variable)
                VariableUses.Add((Variable) opBLo);
            if (opBHi is Variable)
                VariableUses.Add((Variable) opBHi);
            VariableDefs.Add(dstLo);
            VariableDefs.Add(dstHi);
        }

        public override IEnumerable<IRInstruction> GetIRInstructions(IRContext context, IRBasicBlock parentBlock)
        {
            var a = context.VariableMapping[(Variable) Operands[2].op].Cast(IRPrimitive.U64) |
                    context.VariableMapping[(Variable) Operands[3].op].Cast(IRPrimitive.U64).ShiftLeft(32);
            IRExpression b;
            if (Operands[4].op is Constant && Operands[5].op is Constant)
                b = ((Constant) Operands[4].op).Value | ((ulong) ((Constant) Operands[5].op).Value << 32);
            else if (Operands[4].op is Constant && Operands[5].op is Variable)
            {
                uint constant = ((Constant) Operands[4].op).Value;
                if (constant == 0)
                    b = context.VariableMapping[(Variable) Operands[5].op].Cast(IRPrimitive.U64).ShiftLeft(32);
                else
                    b = (ulong) ((Constant) Operands[4].op).Value |
                        context.VariableMapping[(Variable) Operands[5].op].Cast(IRPrimitive.U64).ShiftLeft(32);
            }
            else if (Operands[4].op is Variable && Operands[5].op is Constant)
            {
                uint constant = ((Constant) Operands[5].op).Value;
                if (constant == 0)
                    b = context.VariableMapping[(Variable) Operands[4].op].Cast(IRPrimitive.U64);
                else
                    b = context.VariableMapping[(Variable) Operands[4].op].Cast(IRPrimitive.U64) |
                        ((ulong) ((Constant) Operands[5].op).Value << 32);
            }
            else
                b = context.VariableMapping[(Variable) Operands[4].op].Cast(IRPrimitive.U64) |
                    context.VariableMapping[(Variable) Operands[5].op].Cast(IRPrimitive.U64).ShiftLeft(32);

            yield return new IRAssignment(parentBlock,
                context.VariableMapping[(Variable) Operands[0].op],
                (a + b).Cast(IRPrimitive.U32));
            yield return new IRAssignment(parentBlock,
                context.VariableMapping[(Variable) Operands[1].op],
                (a + b).ShiftRightLogical(32).Cast(IRPrimitive.U32));
        }
    }
}