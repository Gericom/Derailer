using System.Linq;
using Gee.External.Capstone;
using Gee.External.Capstone.Arm;
using LibDerailer.CodeGraph.Nodes.IR;

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
            VariableDefs.UnionWith(defs);
            var uses = instruction.Details.AllReadRegisters
                .Where(reg => reg.Id != ArmRegisterId.ARM_REG_PC)
                .Select(reg => regVars[ArmUtil.GetRegisterNumber(reg.Id)]);
            if (instruction.Details.ConditionCode != ArmConditionCode.ARM_CC_AL)
                uses = uses.Append(regVars[16]);
            if (instruction.Id == ArmInstructionId.ARM_INS_BX)
                uses = uses.Append(regVars[ArmUtil.GetRegisterNumber(instruction.Details.Operands[0].Register.Id)]);
            if (instruction.Details.BelongsToGroup(ArmInstructionGroupId.ARM_GRP_CALL))
                uses = uses.Append(regVars[0]).Append(regVars[1]).Append(regVars[2]).Append(regVars[3]);
            VariableUses.UnionWith(uses);

            foreach (var op in Instruction.Details.Operands)
            {
                switch (op.Type)
                {
                    case ArmOperandType.Register:
                        if(op.AccessType == OperandAccessType.Write)
                        {
                            Operands.Add((true,VariableDefs.First(v =>
                                v.Location == VariableLocation.Register &&
                                v.Address == ArmUtil.GetRegisterNumber(op.Register.Id))));
                        }
                        else
                        {
                            Operands.Add((false, VariableUses.First(v =>
                                v.Location == VariableLocation.Register &&
                                v.Address == ArmUtil.GetRegisterNumber(op.Register.Id))));
                        }
                        break;
                    case ArmOperandType.Immediate:
                        Operands.Add((false, new Constant((uint) op.Immediate)));
                        break;
                    default:
                        Operands.Add((false, null));
                        break;
                }
            }

            if (VariableUses.Contains(regVars[16]))
                FlagsUseOperand = regVars[16];

            if (VariableDefs.Contains(regVars[16]))
                FlagsDefOperand = regVars[16];
        }

        public override string ToString() => Instruction.Mnemonic + " " + Instruction.Operand;

        // public Instruction[] ConvertToIR()
        // {
        //     switch (Instruction.Id)
        //     {
        //         case ArmInstructionId.ARM_INS_AND:
        //         case ArmInstructionId.ARM_INS_EOR:
        //         case ArmInstructionId.ARM_INS_SUB:
        //         case ArmInstructionId.ARM_INS_ADD:
        //         case ArmInstructionId.ARM_INS_ORR:
        //         case ArmInstructionId.ARM_INS_BIC:
        //         {
        //             var opA = VariableUses.FirstOrDefault(v =>
        //                 v.Location == VariableLocation.Register &&
        //                 v.Address == ArmUtil.GetRegisterNumber(Instruction.Details.Operands[1].Register.Id));
        //             Variable opB;
        //             if(Instruction.Details.Operands[2].Type == ArmOperandType.Immediate)
        //             {
        //                 var constVar = new Variable(VariableLocation.None, "constVar", 0, 4);
        //                 var constNode = new LoadConstant(constVar, (uint) Instruction.Details.Operands[2].Immediate);
        //                 opB = constVar;
        //             }
        //             else
        //             {
        //                 var bReg = VariableUses.FirstOrDefault(v =>
        //                     v.Location == VariableLocation.Register &&
        //                     v.Address == ArmUtil.GetRegisterNumber(Instruction.Details.Operands[2].Register.Id));
        //                 //var shiftNode = new Operator();
        //             }
        //             //var opB = VariableUses.FirstOrDefault(v =>
        //             //    v.Location == VariableLocation.Register &&
        //             //    v.Address == ArmUtil.GetRegisterNumber(Instruction.Details.Operands[1].Register.Id));
        //                 //var opB = VariableUses[1];
        //                 //if(Instruction.Details.Operands[1].ShiftOperation == LSL)
        //                 break;
        //         }
        //
        //         case ArmInstructionId.ARM_INS_TST:
        //         case ArmInstructionId.ARM_INS_TEQ:
        //         case ArmInstructionId.ARM_INS_CMP:
        //         case ArmInstructionId.ARM_INS_CMN:
        //             break;
        //         case ArmInstructionId.ARM_INS_MOV:
        //         case ArmInstructionId.ARM_INS_MVN:
        //             break;
        //         case ArmInstructionId.ARM_INS_MUL:
        //             break;
        //     }
        //
        //     return null;
        // }
    }
}