using System.Linq;
using Gee.External.Capstone.Arm;

namespace LibDerailer.CodeGraph.Nodes
{
    public class ArmMachineInstruction : Instruction
    {
        public ArmInstruction Instruction { get; }

        public ArmMachineInstruction(ArmInstruction instruction, Variable[] regVars)
            : base()
        {
            Instruction = instruction;
            var defs = instruction.Details.AllWrittenRegisters
                .Where(reg => reg.Id != ArmRegisterId.ARM_REG_PC)
                .Select(reg => regVars[ArmUtil.GetRegisterNumber(reg.Id)]);
            if (instruction.Details.BelongsToGroup(ArmInstructionGroupId.ARM_GRP_CALL))
                defs = defs.Append(regVars[0]).Append(regVars[1])
                    .Append(regVars[2]).Append(regVars[3]).Append(regVars[16]);
            if (instruction.Details.UpdateFlags && !instruction.Details.IsRegisterWritten(ArmRegisterId.ARM_REG_CPSR))
                defs = defs.Append(regVars[16]);
            VariableDefs.AddRange(defs);
            var uses = instruction.Details.AllReadRegisters
                .Where(reg => reg.Id != ArmRegisterId.ARM_REG_PC)
                .Select(reg => regVars[ArmUtil.GetRegisterNumber(reg.Id)]);
            if (instruction.Details.ConditionCode != ArmConditionCode.ARM_CC_AL)
                uses = uses.Append(regVars[16]);
            if (instruction.Id == ArmInstructionId.ARM_INS_BX)
                uses = uses.Append(regVars[ArmUtil.GetRegisterNumber(instruction.Details.Operands[0].Register.Id)]);
            if (instruction.Details.BelongsToGroup(ArmInstructionGroupId.ARM_GRP_CALL))
                uses = uses.Append(regVars[0]).Append(regVars[1]).Append(regVars[2]).Append(regVars[3]);
            VariableUses.AddRange(uses);
        }

        public override string ToString() => Instruction.Mnemonic + " " + Instruction.Operand;
    }
}