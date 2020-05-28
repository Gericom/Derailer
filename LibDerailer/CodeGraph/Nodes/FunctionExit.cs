using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gee.External.Capstone.Arm;
using LibDerailer.IR;
using LibDerailer.IR.Instructions;

namespace LibDerailer.CodeGraph.Nodes
{
    public class FunctionExit : Instruction
    {
        public FunctionExit(Variable returnValue)
            : base(ArmConditionCode.ARM_CC_AL)
        {
            if (returnValue != null)
                VariableUses.Add(returnValue);
        }

        public override IEnumerable<IRInstruction> GetIRInstructions(IRContext context, IRBasicBlock parentBlock)
        {
            yield return new IRReturn(parentBlock,
                VariableUses.Count == 0 ? null : context.VariableMapping[VariableUses.First()]);
        }
    }
}