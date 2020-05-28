using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gee.External.Capstone.Arm;
using LibDerailer.CCodeGen.Statements;
using LibDerailer.IR;
using LibDerailer.IR.Instructions;

namespace LibDerailer.CodeGraph.Nodes
{
    public class Branch : Instruction
    {
        public BasicBlock Destination { get; set; }

        public Branch(ArmConditionCode condition, BasicBlock destination, Variable cpsr = null)
            : base(condition)
        {
            if(destination == null)
                throw new ArgumentNullException(nameof(destination));
            Destination = destination;
            if (condition != ArmConditionCode.ARM_CC_AL && cpsr != null)
            {
                VariableUses.Add(cpsr);
                FlagsUseOperand = cpsr;
            }
        }

        public override IEnumerable<IRInstruction> GetIRInstructions(IRContext context, IRBasicBlock parentBlock)
        {
            yield return new IRJump(parentBlock, context.BasicBlockMapping[Destination],
                FlagsUseOperand == null || Condition == ArmConditionCode.ARM_CC_AL
                    ? null
                    : VariableUseLocs[(Variable) FlagsUseOperand].First().GetIRPredicateCode(context, Condition));
        }
    }
}