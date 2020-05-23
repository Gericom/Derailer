using System;
using System.Collections.Generic;
using System.Linq;
using Gee.External.Capstone;
using Gee.External.Capstone.Arm;
using LibDerailer.CCodeGen;
using LibDerailer.CCodeGen.Statements;
using LibDerailer.CCodeGen.Statements.Expressions;
using LibDerailer.CodeGraph.Nodes.IR;

namespace LibDerailer.CodeGraph.Nodes
{
    public class ArmMachineInstruction : Instruction
    {
        public ArmInstruction Instruction { get; }

        public ArmMachineInstruction(ArmInstruction instruction, Variable[] regVars)
        : base(instruction.Details.ConditionCode)
        {
            Instruction   = instruction;
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
            if (instruction.Details.BelongsToGroup(ArmInstructionGroupId.ARM_GRP_CALL) ||
                instruction.Id == ArmInstructionId.ARM_INS_BX)
                uses = uses.Append(regVars[0]).Append(regVars[1]).Append(regVars[2]).Append(regVars[3]);
            VariableUses.UnionWith(uses);

            foreach (var op in Instruction.Details.Operands)
            {
                switch (op.Type)
                {
                    case ArmOperandType.Register:
                        if (op.AccessType == OperandAccessType.Write)
                        {
                            Operands.Add((true, VariableDefs.FirstOrDefault(v =>
                                v.Location == VariableLocation.Register &&
                                v.Address == ArmUtil.GetRegisterNumber(op.Register.Id))));
                        }
                        else
                        {
                            Operands.Add((false, VariableUses.FirstOrDefault(v =>
                                v.Location == VariableLocation.Register &&
                                v.Address == ArmUtil.GetRegisterNumber(op.Register.Id))));
                            if (op.ShiftOperation >= ArmShiftOperation.ARM_SFT_LSL_REG)
                            {
                                Operands.Add((false, VariableUses.First(v =>
                                    v.Location == VariableLocation.Register &&
                                    v.Address == ArmUtil.GetRegisterNumber(op.ShiftRegister.Id))));
                            }
                        }

                        break;
                    case ArmOperandType.Memory:
                        Operands.Add((false, VariableUses.First(v =>
                            v.Location == VariableLocation.Register &&
                            v.Address == ArmUtil.GetRegisterNumber(op.Memory.Base.Id))));
                        if (op.Memory.Index == null)
                            Operands.Add((false, new Constant((uint) op.Memory.Displacement)));
                        else
                            Operands.Add((false, VariableUses.First(v =>
                                v.Location == VariableLocation.Register &&
                                v.Address == ArmUtil.GetRegisterNumber(op.Memory.Index.Id))));
                        if (instruction.Details.WriteBack)
                            Operands.Add((true, VariableDefs.First(v =>
                                v.Location == VariableLocation.Register &&
                                v.Address == ArmUtil.GetRegisterNumber(op.Memory.Base.Id))));
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

        private CExpression GetOperand(int idx)
        {
            return new CVariable(((Variable) Operands[idx].op).Name);
        }

        private CExpression GetSecondOperand(int idx)
        {
            if (Instruction.Details.Operands[idx].Type == ArmOperandType.Immediate)
                return (uint) Instruction.Details.Operands[idx].Immediate;

            var rm = new CVariable(((Variable) Operands[idx].op).Name);

            if (Instruction.Details.Operands[idx].ShiftOperation == ArmShiftOperation.Invalid ||
                (Instruction.Details.Operands[idx].ShiftOperation == ArmShiftOperation.ARM_SFT_LSL &&
                 Instruction.Details.Operands[idx].ShiftValue == 0))
                return rm;

            CCodeGen.Statements.Expressions.CVariable rs = null;
            if (Instruction.Details.Operands[idx].ShiftOperation >= ArmShiftOperation.ARM_SFT_LSL_REG)
                rs = new CVariable(((Variable) Operands[idx + 1].op).Name);

            switch (Instruction.Details.Operands[idx].ShiftOperation)
            {
                case ArmShiftOperation.ARM_SFT_LSL:
                    return rm << Instruction.Details.Operands[idx].ShiftValue;
                case ArmShiftOperation.ARM_SFT_LSL_REG:
                    return new CMethodCall(true, "<<", rm, rs);
                case ArmShiftOperation.ARM_SFT_LSR:
                    return rm >> Instruction.Details.Operands[idx].ShiftValue;
                case ArmShiftOperation.ARM_SFT_LSR_REG:
                    return new CMethodCall(true, ">>", rm, rs);
                case ArmShiftOperation.ARM_SFT_ASR:
                    return new CCast(new CType("int"), rm) >> Instruction.Details.Operands[idx].ShiftValue;
                case ArmShiftOperation.ARM_SFT_ASR_REG:
                    return new CMethodCall(true, ">>", new CCast(new CType("int"), rm), rs);
                default:
                    throw new NotImplementedException("Unimplemented shift!");
            }
        }

        public override CStatement[] GetCode()
        {
            switch (Instruction.Id)
            {
                case ArmInstructionId.ARM_INS_ADD:
                    return new CStatement[] {CExpression.Assign(GetOperand(0), GetOperand(1) + GetSecondOperand(2))};
                case ArmInstructionId.ARM_INS_SUB:
                    return new CStatement[] {CExpression.Assign(GetOperand(0), GetOperand(1) - GetSecondOperand(2))};
                case ArmInstructionId.ARM_INS_RSB:
                    return new CStatement[] {CExpression.Assign(GetOperand(0), GetSecondOperand(2) - GetOperand(1))};
                case ArmInstructionId.ARM_INS_AND:
                    return new CStatement[] {CExpression.Assign(GetOperand(0), GetOperand(1) & GetSecondOperand(2))};
                case ArmInstructionId.ARM_INS_ORR:
                    return new CStatement[] {CExpression.Assign(GetOperand(0), GetOperand(1) | GetSecondOperand(2))};
                case ArmInstructionId.ARM_INS_EOR:
                    return new CStatement[] {CExpression.Assign(GetOperand(0), GetOperand(1) ^ GetSecondOperand(2))};
                case ArmInstructionId.ARM_INS_BIC:
                    return new CStatement[] {CExpression.Assign(GetOperand(0), GetOperand(1) & ~GetSecondOperand(2))};
                case ArmInstructionId.ARM_INS_LSL:
                case ArmInstructionId.ARM_INS_LSR:
                case ArmInstructionId.ARM_INS_ASR:
                case ArmInstructionId.ARM_INS_ROR:
                case ArmInstructionId.ARM_INS_RRX:
                case ArmInstructionId.ARM_INS_MOV:
                    return new CStatement[] {CExpression.Assign(GetOperand(0), GetSecondOperand(1))};
                case ArmInstructionId.ARM_INS_MVN:
                    return new CStatement[] {CExpression.Assign(GetOperand(0), ~GetSecondOperand(1))};
                case ArmInstructionId.ARM_INS_MUL:
                    return new CStatement[] {CExpression.Assign(GetOperand(0), GetOperand(1) * GetOperand(2))};
                case ArmInstructionId.ARM_INS_MLA:
                    return new CStatement[]
                        {CExpression.Assign(GetOperand(0), GetOperand(1) * GetOperand(2) + GetOperand(3))};
                case ArmInstructionId.ARM_INS_LDR:
                {
                    if (Instruction.Details.WriteBack)
                        throw new NotImplementedException("Unimplemented instruction!");
                    if (Instruction.Details.Operands[1].Memory.Index == null &&
                        Instruction.Details.Operands[1].Memory.Displacement == 0)
                        return new CStatement[]
                        {
                            CExpression.Assign(GetOperand(0),
                                CExpression.Deref(new CCast(new CType("u32", true), GetOperand(1))))
                        };
                    else if (Instruction.Details.Operands[1].Memory.Index == null)
                        return new CStatement[]
                        {
                            CExpression.Assign(GetOperand(0),
                                CExpression.Deref(new CCast(new CType("u32", true),
                                    GetOperand(1) + Instruction.Details.Operands[1].Memory.Displacement)))
                        };
                    else
                    {
                        throw new NotImplementedException("Unimplemented instruction!");
                    }
                }
                case ArmInstructionId.ARM_INS_STR:
                {
                    if (Instruction.Details.WriteBack)
                        throw new NotImplementedException("Unimplemented instruction!");
                    if (Instruction.Details.Operands[1].Memory.Index == null &&
                        Instruction.Details.Operands[1].Memory.Displacement == 0)
                        return new CStatement[]
                        {
                            CExpression.Assign(
                                CExpression.Deref(new CCast(new CType("u32", true), GetOperand(1))), GetOperand(0))
                        };
                    else if (Instruction.Details.Operands[1].Memory.Index == null)
                        return new CStatement[]
                        {
                            CExpression.Assign(
                                CExpression.Deref(new CCast(new CType("u32", true),
                                    GetOperand(1) + Instruction.Details.Operands[1].Memory.Displacement)),
                                GetOperand(0))
                        };
                    else
                    {
                        throw new NotImplementedException("Unimplemented instruction!");
                    }
                }
                case ArmInstructionId.ARM_INS_PUSH:
                case ArmInstructionId.ARM_INS_POP:
                case ArmInstructionId.ARM_INS_CMP:
                case ArmInstructionId.ARM_INS_B:
                    return new CStatement[0];
                default:
                    throw new NotImplementedException("Unimplemented instruction!");
            }
        }
    }
}