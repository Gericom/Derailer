using System.Collections.Generic;
using System.Linq;
using Gee.External.Capstone.Arm;
using LibDerailer.IR;
using LibDerailer.IR.Instructions;

namespace LibDerailer.CodeGraph.Nodes
{
    public class LoadConstant : Instruction
    {
        public uint Constant { get; }

        public LoadConstant(uint address, ArmConditionCode condition, Variable dst, uint constant, Variable cpsr = null)
            : base(address, condition)
        {
            Constant = constant;
            VariableDefs.Add(dst);
            Operands.Add((true, dst));
            if (condition != ArmConditionCode.ARM_CC_AL && cpsr != null)
            {
                VariableUses.Add(cpsr);
                FlagsUseOperand = cpsr;
            }
        }

        public override string ToString() => $"{Operands[0].op} = 0x{Constant:X08}";

        public override IEnumerable<IRInstruction> GetIRInstructions(IRContext context, IRBasicBlock parentBlock)
        {
            yield return new IRAssignment(parentBlock, context.VariableMapping[(Variable) Operands[0].op], Constant);
        }
    }
}