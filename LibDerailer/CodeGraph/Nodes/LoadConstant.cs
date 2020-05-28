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

        public LoadConstant(ArmConditionCode condition, Variable dst, uint constant)
            : base(condition)
        {
            Constant = constant;
            VariableDefs.Add(dst);
            Operands.Add((true, dst));
        }

        public override string ToString() => $"{Operands[0].op} = 0x{Constant:X08}";

        public override IEnumerable<IRInstruction> GetIRInstructions(IRContext context, IRBasicBlock parentBlock)
        {
            yield return new IRAssignment(parentBlock, context.VariableMapping[(Variable)Operands[0].op], Constant);
        }
    }
}