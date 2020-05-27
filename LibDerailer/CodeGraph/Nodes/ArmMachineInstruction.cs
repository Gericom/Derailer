using System;
using System.Collections.Generic;
using System.Linq;
using Gee.External.Capstone;
using Gee.External.Capstone.Arm;
using LibDerailer.CCodeGen;
using LibDerailer.CCodeGen.Statements;
using LibDerailer.CCodeGen.Statements.Expressions;
using LibDerailer.IR;
using LibDerailer.IR.Expressions;
using LibDerailer.IR.Instructions;

namespace LibDerailer.CodeGraph.Nodes
{
    public class ArmMachineInstruction : Instruction
    {
        public ArmInstruction Instruction { get; }

        public ArmMachineInstruction(ArmInstruction instruction, Variable[] regVars)
            : base(instruction.Details.ConditionCode)
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
            if (instruction.Details.BelongsToGroup(ArmInstructionGroupId.ARM_GRP_CALL) ||
                instruction.Id == ArmInstructionId.ARM_INS_BX)
                uses = uses.Append(regVars[0]).Append(regVars[1]).Append(regVars[2]).Append(regVars[3])
                    .Append(regVars[13]);
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

        private CExpression GetOperand(int idx)
        {
            return new CVariable(((Variable) Operands[idx].op).Name);
        }

        private IRVariable GetIROperand(IRContext context, int idx)
        {
            return context.VariableMapping[(Variable) Operands[idx].op];
        }

        private IRExpression GetIRSecondOperand(IRContext context, int idx)
        {
            if (Instruction.Details.Operands[idx].Type == ArmOperandType.Immediate)
                return (uint) Instruction.Details.Operands[idx].Immediate;

            var rm = context.VariableMapping[(Variable) Operands[idx].op];

            if (Instruction.Details.Operands[idx].ShiftOperation == ArmShiftOperation.Invalid ||
                (Instruction.Details.Operands[idx].ShiftOperation == ArmShiftOperation.ARM_SFT_LSL &&
                 Instruction.Details.Operands[idx].ShiftValue == 0))
                return rm;

            IRVariable rs = null;
            if (Instruction.Details.Operands[idx].ShiftOperation >= ArmShiftOperation.ARM_SFT_LSL_REG)
                rs = context.VariableMapping[(Variable) Operands[idx + 1].op];

            switch (Instruction.Details.Operands[idx].ShiftOperation)
            {
                case ArmShiftOperation.ARM_SFT_LSL:
                    return rm.ShiftLeft(Instruction.Details.Operands[idx].ShiftValue);
                case ArmShiftOperation.ARM_SFT_LSL_REG:
                    return rm.ShiftLeft(rs);
                case ArmShiftOperation.ARM_SFT_LSR:
                    return rm.ShiftRightLogical(Instruction.Details.Operands[idx].ShiftValue);
                case ArmShiftOperation.ARM_SFT_LSR_REG:
                    return rm.ShiftRightLogical(rs);
                case ArmShiftOperation.ARM_SFT_ASR:
                    return rm.ShiftRightArithmetic(Instruction.Details.Operands[idx].ShiftValue);
                case ArmShiftOperation.ARM_SFT_ASR_REG:
                    return rm.ShiftRightArithmetic(rs);
                default:
                    throw new NotImplementedException("Unimplemented shift!");
            }
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

        public override IEnumerable<IRInstruction> GetIRInstructions(IRContext context, IRBasicBlock parentBlock)
        {
            switch (Instruction.Id)
            {
                case ArmInstructionId.ARM_INS_ADD:
                    yield return new IRAssignment(parentBlock, GetIROperand(context, 0),
                        GetIROperand(context, 1) + GetIRSecondOperand(context, 2));
                    break;
                case ArmInstructionId.ARM_INS_SUB:
                    yield return new IRAssignment(parentBlock, GetIROperand(context, 0),
                        GetIROperand(context, 1) - GetIRSecondOperand(context, 2));
                    break;
                case ArmInstructionId.ARM_INS_RSB:
                    yield return new IRAssignment(parentBlock, GetIROperand(context, 0),
                        GetIRSecondOperand(context, 2) - GetIROperand(context, 1));
                    break;
                case ArmInstructionId.ARM_INS_AND:
                    yield return new IRAssignment(parentBlock, GetIROperand(context, 0),
                        GetIROperand(context, 1) & GetIRSecondOperand(context, 2));
                    break;
                case ArmInstructionId.ARM_INS_ORR:
                    yield return new IRAssignment(parentBlock, GetIROperand(context, 0),
                        GetIROperand(context, 1) | GetIRSecondOperand(context, 2));
                    break;
                case ArmInstructionId.ARM_INS_EOR:
                    yield return new IRAssignment(parentBlock, GetIROperand(context, 0),
                        GetIROperand(context, 1) ^ GetIRSecondOperand(context, 2));
                    break;
                case ArmInstructionId.ARM_INS_BIC:
                    yield return new IRAssignment(parentBlock, GetIROperand(context, 0),
                        GetIROperand(context, 1) & ~GetIRSecondOperand(context, 2));
                    break;
                case ArmInstructionId.ARM_INS_LSL:
                case ArmInstructionId.ARM_INS_LSR:
                case ArmInstructionId.ARM_INS_ASR:
                case ArmInstructionId.ARM_INS_ROR:
                case ArmInstructionId.ARM_INS_RRX:
                case ArmInstructionId.ARM_INS_MOV:
                    yield return new IRAssignment(parentBlock, GetIROperand(context, 0),
                        GetIRSecondOperand(context, 1));
                    break;
                case ArmInstructionId.ARM_INS_MVN:
                    yield return new IRAssignment(parentBlock, GetIROperand(context, 0),
                        ~GetIRSecondOperand(context, 1));
                    break;
                case ArmInstructionId.ARM_INS_MUL:
                    yield return new IRAssignment(parentBlock, GetIROperand(context, 0),
                        GetIROperand(context, 1) * GetIROperand(context, 2));
                    break;
                case ArmInstructionId.ARM_INS_MLA:
                    yield return new IRAssignment(parentBlock, GetIROperand(context, 0),
                        GetIROperand(context, 1) * GetIROperand(context, 2) + GetIROperand(context, 3));
                    break;
                // case ArmInstructionId.ARM_INS_SMULL:
                //     yield return new IRAssignment(GetIROperand(0), GetIROperand(1).Sext(IRType.I64) * GetIROperand(2).Sext(IRType.I64));
                //     break;
                case ArmInstructionId.ARM_INS_LDR:
                case ArmInstructionId.ARM_INS_LDRH:
                case ArmInstructionId.ARM_INS_LDRSH:
                case ArmInstructionId.ARM_INS_LDRB:
                case ArmInstructionId.ARM_INS_LDRSB:
                {
                    bool   signed = false;
                    IRType type   = IRType.Void;
                    switch (Instruction.Id)
                    {
                        case ArmInstructionId.ARM_INS_LDR:
                            type = IRType.I32;
                            break;
                        case ArmInstructionId.ARM_INS_LDRH:
                            type = IRType.I16;
                            break;
                        case ArmInstructionId.ARM_INS_LDRSH:
                            type   = IRType.I16;
                            signed = true;
                            break;
                        case ArmInstructionId.ARM_INS_LDRB:
                            type = IRType.I8;
                            break;
                        case ArmInstructionId.ARM_INS_LDRSB:
                            type   = IRType.I8;
                            signed = true;
                            break;
                    }

                    if (Instruction.Details.WriteBack)
                        throw new NotImplementedException("Unimplemented instruction!");


                    if (Instruction.Details.Operands[1].Memory.Base.Id == ArmRegisterId.ARM_REG_SP)
                    {
                        yield return new IRAssignment(parentBlock,
                            context.VariableMapping[VariableUses.First(v => v.Location == VariableLocation.Stack)],
                            GetIROperand(context, 0));
                        break;
                    }

                    if (Instruction.Details.Operands[1].Memory.Index == null &&
                        Instruction.Details.Operands[1].Memory.Displacement == 0)
                    {
                        yield return new IRLoadStore(parentBlock, false, type, signed, GetIROperand(context, 1),
                            GetIROperand(context, 0));
                        break;
                    }
                    else if (Instruction.Details.Operands[1].Memory.Index == null)
                    {
                        yield return new IRLoadStore(parentBlock, false, type, signed,
                            GetIROperand(context, 1) + Instruction.Details.Operands[1].Memory.Displacement,
                            GetIROperand(context, 0));
                        break;
                    }
                    else
                    {
                        if (Instruction.Details.Operands[1].ShiftOperation == ArmShiftOperation.Invalid ||
                            (Instruction.Details.Operands[1].ShiftOperation == ArmShiftOperation.ARM_SFT_LSL &&
                             Instruction.Details.Operands[1].ShiftValue == 0))
                        {
                            yield return new IRLoadStore(parentBlock, false, type, signed,
                                GetIROperand(context, 1) + GetIROperand(context, 2),
                                GetIROperand(context, 0));
                            break;
                        }
                        else
                            throw new NotImplementedException("Unimplemented instruction!");
                    }
                }
                case ArmInstructionId.ARM_INS_STR:
                case ArmInstructionId.ARM_INS_STRH:
                case ArmInstructionId.ARM_INS_STRB:
                    {
                        IRType type = IRType.Void;
                        switch (Instruction.Id)
                        {
                            case ArmInstructionId.ARM_INS_STR:
                                type = IRType.I32;
                                break;
                            case ArmInstructionId.ARM_INS_STRH:
                                type = IRType.I16;
                                break;
                            case ArmInstructionId.ARM_INS_STRB:
                                type = IRType.I8;
                                break;
                        }

                        if (Instruction.Details.WriteBack)
                            throw new NotImplementedException("Unimplemented instruction!");


                        if (Instruction.Details.Operands[1].Memory.Base.Id == ArmRegisterId.ARM_REG_SP)
                        {
                            yield return new IRAssignment(parentBlock,
                                context.VariableMapping[VariableDefs.First(v => v.Location == VariableLocation.Stack)],
                                GetIROperand(context, 0));
                            break;
                        }

                        if (Instruction.Details.Operands[1].Memory.Index == null &&
                            Instruction.Details.Operands[1].Memory.Displacement == 0)
                        {
                            yield return new IRLoadStore(parentBlock, true, type, false, GetIROperand(context, 1),
                                GetIROperand(context, 0));
                            break;
                        }
                        else if (Instruction.Details.Operands[1].Memory.Index == null)
                        {
                            yield return new IRLoadStore(parentBlock, true, type, false,
                                GetIROperand(context, 1) + Instruction.Details.Operands[1].Memory.Displacement,
                                GetIROperand(context, 0));
                            break;
                        }
                        else
                        {
                            if (Instruction.Details.Operands[1].ShiftOperation == ArmShiftOperation.Invalid ||
                                (Instruction.Details.Operands[1].ShiftOperation == ArmShiftOperation.ARM_SFT_LSL &&
                                 Instruction.Details.Operands[1].ShiftValue == 0))
                            {
                                yield return new IRLoadStore(parentBlock, true, type, false,
                                    GetIROperand(context, 1) + GetIROperand(context, 2),
                                    GetIROperand(context, 0));
                                break;
                            }
                            else
                                throw new NotImplementedException("Unimplemented instruction!");
                        }
                    }
                case ArmInstructionId.ARM_INS_BL:
                    yield return new IRAssignment(parentBlock,
                        context.VariableMapping[VariableDefs
                            .First(v => v.Location == VariableLocation.Register && v.Address == 0)],
                        new IRCallExpression(IRType.I32, $"sub_{Instruction.Details.Operands[0].Immediate:X08}",
                            context.VariableMapping[VariableUses
                                .First(v => v.Location == VariableLocation.Register && v.Address == 0)],
                            context.VariableMapping[VariableUses
                                .First(v => v.Location == VariableLocation.Register && v.Address == 1)],
                            context.VariableMapping[VariableUses
                                .First(v => v.Location == VariableLocation.Register && v.Address == 2)],
                            context.VariableMapping[VariableUses
                                .First(v => v.Location == VariableLocation.Register && v.Address == 3)]
                        ));
                    break;
                case ArmInstructionId.ARM_INS_BLX:
                    if (Instruction.Details.Operands[0].Type == ArmOperandType.Immediate)
                        goto case ArmInstructionId.ARM_INS_BL;
                    yield return new IRAssignment(parentBlock,
                        context.VariableMapping[VariableDefs
                            .First(v => v.Location == VariableLocation.Register && v.Address == 0)],
                        new IRCallExpression(IRType.I32, ((Variable) Operands[0].op).Name,
                            context.VariableMapping[VariableUses
                                .First(v => v.Location == VariableLocation.Register && v.Address == 0)],
                            context.VariableMapping[VariableUses
                                .First(v => v.Location == VariableLocation.Register && v.Address == 1)],
                            context.VariableMapping[VariableUses
                                .First(v => v.Location == VariableLocation.Register && v.Address == 2)],
                            context.VariableMapping[VariableUses
                                .First(v => v.Location == VariableLocation.Register && v.Address == 3)]
                        ));
                    break;
                case ArmInstructionId.ARM_INS_LDM:
                    if (Instruction.Details.Operands[0].Register.Id == ArmRegisterId.ARM_REG_SP &&
                        Instruction.Details.WriteBack)
                        break;
                    goto default;
                case ArmInstructionId.ARM_INS_STMDB:
                    if (Instruction.Details.Operands[0].Register.Id == ArmRegisterId.ARM_REG_SP &&
                        Instruction.Details.WriteBack)
                        break;
                    goto default;
                case ArmInstructionId.ARM_INS_PUSH:
                case ArmInstructionId.ARM_INS_POP:
                case ArmInstructionId.ARM_INS_CMP:
                case ArmInstructionId.ARM_INS_B:
                case ArmInstructionId.ARM_INS_BX:
                    break;
                default:
                    throw new NotImplementedException("Unimplemented instruction!");
            }
        }

        public override IRExpression GetIRPredicateCode(IRContext context, ArmConditionCode condition)
        {
            switch (Instruction.Id)
            {
                case ArmInstructionId.ARM_INS_AND:
                case ArmInstructionId.ARM_INS_LSR:
                    switch (condition)
                    {
                        case ArmConditionCode.ARM_CC_EQ:
                            return GetIROperand(context, 0) == 0;
                        case ArmConditionCode.ARM_CC_NE:
                            return GetIROperand(context, 0) != 0;
                        case ArmConditionCode.ARM_CC_HS:
                        case ArmConditionCode.ARM_CC_LO:
                        case ArmConditionCode.ARM_CC_MI:
                        case ArmConditionCode.ARM_CC_PL:
                        case ArmConditionCode.ARM_CC_VS:
                        case ArmConditionCode.ARM_CC_VC:
                        case ArmConditionCode.ARM_CC_HI:
                        case ArmConditionCode.ARM_CC_LS:
                        case ArmConditionCode.ARM_CC_GE:
                        case ArmConditionCode.ARM_CC_LT:
                        case ArmConditionCode.ARM_CC_GT:
                        case ArmConditionCode.ARM_CC_LE:
                            throw new NotImplementedException("Unimplemented and condition");
                        case ArmConditionCode.ARM_CC_AL:
                            return true;
                        default:
                            throw new ArgumentException("Invalid condition!");
                    }

                case ArmInstructionId.ARM_INS_CMP:
                case ArmInstructionId.ARM_INS_SUB:
                    switch (condition)
                    {
                        case ArmConditionCode.ARM_CC_EQ:
                            return GetIROperand(context, 0) == GetIRSecondOperand(context, 1);
                        case ArmConditionCode.ARM_CC_NE:
                            return GetIROperand(context, 0) != GetIRSecondOperand(context, 1);
                        case ArmConditionCode.ARM_CC_HS:
                            return GetIROperand(context, 0).UnsignedGreaterEqualAs(GetIRSecondOperand(context, 1));
                        case ArmConditionCode.ARM_CC_LO:
                            return GetIROperand(context, 0).UnsignedLessThan(GetIRSecondOperand(context, 1));
                        case ArmConditionCode.ARM_CC_MI:
                            return (GetIROperand(context, 0) - GetIRSecondOperand(context, 1)).LessThan(0);
                        case ArmConditionCode.ARM_CC_PL:
                            return (GetIROperand(context, 0) - GetIRSecondOperand(context, 1)).GreaterThan(0);
                        case ArmConditionCode.ARM_CC_VS:
                            throw new NotImplementedException("Unimplemented cmp condition");
                        case ArmConditionCode.ARM_CC_VC:
                            throw new NotImplementedException("Unimplemented cmp condition");
                        case ArmConditionCode.ARM_CC_HI:
                            return GetIROperand(context, 0).UnsignedGreaterThan(GetIRSecondOperand(context, 1));
                        case ArmConditionCode.ARM_CC_LS:
                            return GetIROperand(context, 0).UnsignedLessEqualAs(GetIRSecondOperand(context, 1));
                        case ArmConditionCode.ARM_CC_GE:
                            return GetIROperand(context, 0).GreaterEqualAs(GetIRSecondOperand(context, 1));
                        case ArmConditionCode.ARM_CC_LT:
                            return GetIROperand(context, 0).LessThan(GetIRSecondOperand(context, 1));
                        case ArmConditionCode.ARM_CC_GT:
                            return GetIROperand(context, 0).GreaterThan(GetIRSecondOperand(context, 1));
                        case ArmConditionCode.ARM_CC_LE:
                            return GetIROperand(context, 0).LessEqualAs(GetIRSecondOperand(context, 1));
                        case ArmConditionCode.ARM_CC_AL:
                            return true;
                        default:
                            throw new ArgumentException("Invalid condition!");
                    }

                default:
                    throw new NotImplementedException("Unimplemented instruction!");
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
                case ArmInstructionId.ARM_INS_LDRH:
                case ArmInstructionId.ARM_INS_LDRSH:
                case ArmInstructionId.ARM_INS_LDRB:
                {
                    CType ptrType = null;
                    switch (Instruction.Id)
                    {
                        case ArmInstructionId.ARM_INS_LDR:
                            ptrType = new CType("u32", true);
                            break;
                        case ArmInstructionId.ARM_INS_LDRH:
                            ptrType = new CType("u16", true);
                            break;
                        case ArmInstructionId.ARM_INS_LDRSH:
                            ptrType = new CType("s16", true);
                            break;
                        case ArmInstructionId.ARM_INS_LDRB:
                            ptrType = new CType("u8", true);
                            break;
                    }

                    if (Instruction.Details.WriteBack)
                        throw new NotImplementedException("Unimplemented instruction!");


                    if (Instruction.Details.Operands[1].Memory.Base.Id == ArmRegisterId.ARM_REG_SP)
                    {
                        return new CStatement[]
                        {
                            CExpression.Assign(GetOperand(0),
                                new CVariable(VariableUses.First(v => v.Location == VariableLocation.Stack).Name))
                        };
                    }

                    if (Instruction.Details.Operands[1].Memory.Index == null &&
                        Instruction.Details.Operands[1].Memory.Displacement == 0)
                        return new CStatement[]
                        {
                            CExpression.Assign(GetOperand(0),
                                CExpression.Deref(new CCast(ptrType, GetOperand(1))))
                        };
                    else if (Instruction.Details.Operands[1].Memory.Index == null)
                        return new CStatement[]
                        {
                            CExpression.Assign(GetOperand(0),
                                CExpression.Deref(new CCast(ptrType,
                                    GetOperand(1) + Instruction.Details.Operands[1].Memory.Displacement)))
                        };
                    else
                    {
                        if (Instruction.Details.Operands[1].ShiftOperation == ArmShiftOperation.Invalid ||
                            (Instruction.Details.Operands[1].ShiftOperation == ArmShiftOperation.ARM_SFT_LSL &&
                             Instruction.Details.Operands[1].ShiftValue == 0))
                            return new CStatement[]
                            {
                                CExpression.Assign(GetOperand(0),
                                    CExpression.Deref(new CCast(ptrType,
                                        GetOperand(1) + GetOperand(2))))
                            };
                        else
                            throw new NotImplementedException("Unimplemented instruction!");
                    }
                }
                case ArmInstructionId.ARM_INS_STR:
                case ArmInstructionId.ARM_INS_STRH:
                case ArmInstructionId.ARM_INS_STRB:
                {
                    CType ptrType = null;
                    switch (Instruction.Id)
                    {
                        case ArmInstructionId.ARM_INS_STR:
                            ptrType = new CType("u32", true);
                            break;
                        case ArmInstructionId.ARM_INS_STRH:
                            ptrType = new CType("u16", true);
                            break;
                        case ArmInstructionId.ARM_INS_STRB:
                            ptrType = new CType("u8", true);
                            break;
                    }

                    if (Instruction.Details.WriteBack)
                        throw new NotImplementedException("Unimplemented instruction!");

                    if (Instruction.Details.Operands[1].Memory.Base.Id == ArmRegisterId.ARM_REG_SP)
                    {
                        return new CStatement[]
                        {
                            CExpression.Assign(
                                new CVariable(VariableDefs.First(v => v.Location == VariableLocation.Stack).Name),
                                GetOperand(0))
                        };
                    }

                    if (Instruction.Details.Operands[1].Memory.Index == null &&
                        Instruction.Details.Operands[1].Memory.Displacement == 0)
                        return new CStatement[]
                        {
                            CExpression.Assign(
                                CExpression.Deref(new CCast(ptrType, GetOperand(1))), GetOperand(0))
                        };
                    else if (Instruction.Details.Operands[1].Memory.Index == null)
                        return new CStatement[]
                        {
                            CExpression.Assign(
                                CExpression.Deref(new CCast(ptrType,
                                    GetOperand(1) + Instruction.Details.Operands[1].Memory.Displacement)),
                                GetOperand(0))
                        };
                    else
                    {
                        throw new NotImplementedException("Unimplemented instruction!");
                    }
                }
                case ArmInstructionId.ARM_INS_BL:
                    return new CStatement[]
                    {
                        CExpression.Assign(
                            new CVariable(VariableDefs
                                .First(v => v.Location == VariableLocation.Register && v.Address == 0).Name),
                            new CMethodCall(false, $"sub_{Instruction.Details.Operands[0].Immediate:X08}",
                                new CVariable(VariableUses
                                    .First(v => v.Location == VariableLocation.Register && v.Address == 0).Name),
                                new CVariable(VariableUses
                                    .First(v => v.Location == VariableLocation.Register && v.Address == 1).Name),
                                new CVariable(VariableUses
                                    .First(v => v.Location == VariableLocation.Register && v.Address == 2).Name),
                                new CVariable(VariableUses
                                    .First(v => v.Location == VariableLocation.Register && v.Address == 3).Name)
                            ))
                    };
                case ArmInstructionId.ARM_INS_BLX:
                    if (Instruction.Details.Operands[0].Type == ArmOperandType.Immediate)
                        goto case ArmInstructionId.ARM_INS_BL;
                    return new CStatement[]
                    {
                        CExpression.Assign(
                            new CVariable(VariableDefs
                                .First(v => v.Location == VariableLocation.Register && v.Address == 0).Name),
                            new CMethodCall(false, ((Variable) Operands[0].op).Name,
                                new CVariable(VariableUses
                                    .First(v => v.Location == VariableLocation.Register && v.Address == 0).Name),
                                new CVariable(VariableUses
                                    .First(v => v.Location == VariableLocation.Register && v.Address == 1).Name),
                                new CVariable(VariableUses
                                    .First(v => v.Location == VariableLocation.Register && v.Address == 2).Name),
                                new CVariable(VariableUses
                                    .First(v => v.Location == VariableLocation.Register && v.Address == 3).Name)
                            ))
                    };
                case ArmInstructionId.ARM_INS_LDM:
                    if (Instruction.Details.Operands[0].Register.Id == ArmRegisterId.ARM_REG_SP &&
                        Instruction.Details.WriteBack)
                        return new CStatement[0];
                    goto default;
                case ArmInstructionId.ARM_INS_STMDB:
                    if (Instruction.Details.Operands[0].Register.Id == ArmRegisterId.ARM_REG_SP &&
                        Instruction.Details.WriteBack)
                        return new CStatement[0];
                    goto default;
                case ArmInstructionId.ARM_INS_PUSH:
                case ArmInstructionId.ARM_INS_POP:
                case ArmInstructionId.ARM_INS_CMP:
                case ArmInstructionId.ARM_INS_B:
                case ArmInstructionId.ARM_INS_BX:
                    return new CStatement[0];
                default:
                    throw new NotImplementedException("Unimplemented instruction!");
            }
        }

        public override CExpression GetPredicateCode(ArmConditionCode condition)
        {
            switch (Instruction.Id)
            {
                case ArmInstructionId.ARM_INS_AND:
                case ArmInstructionId.ARM_INS_LSR:
                    switch (condition)
                    {
                        case ArmConditionCode.ARM_CC_EQ:
                            return GetOperand(0) == 0;
                        case ArmConditionCode.ARM_CC_NE:
                            return GetOperand(0) != 0;
                        case ArmConditionCode.ARM_CC_HS:
                        case ArmConditionCode.ARM_CC_LO:
                        case ArmConditionCode.ARM_CC_MI:
                        case ArmConditionCode.ARM_CC_PL:
                        case ArmConditionCode.ARM_CC_VS:
                        case ArmConditionCode.ARM_CC_VC:
                        case ArmConditionCode.ARM_CC_HI:
                        case ArmConditionCode.ARM_CC_LS:
                        case ArmConditionCode.ARM_CC_GE:
                        case ArmConditionCode.ARM_CC_LT:
                        case ArmConditionCode.ARM_CC_GT:
                        case ArmConditionCode.ARM_CC_LE:
                            throw new NotImplementedException("Unimplemented and condition");
                        case ArmConditionCode.ARM_CC_AL:
                            return true;
                        default:
                            throw new ArgumentException("Invalid condition!");
                    }

                case ArmInstructionId.ARM_INS_CMP:
                    switch (condition)
                    {
                        case ArmConditionCode.ARM_CC_EQ:
                            return GetOperand(0) == GetSecondOperand(1);
                        case ArmConditionCode.ARM_CC_NE:
                            return GetOperand(0) != GetSecondOperand(1);
                        case ArmConditionCode.ARM_CC_HS:
                            return GetOperand(0) >= GetSecondOperand(1);
                        case ArmConditionCode.ARM_CC_LO:
                            return GetOperand(0) < GetSecondOperand(1);
                        case ArmConditionCode.ARM_CC_MI:
                            return new CCast(new CType("int"), GetOperand(0) - GetSecondOperand(1)) < 0;
                        case ArmConditionCode.ARM_CC_PL:
                            return new CCast(new CType("int"), GetOperand(0) - GetSecondOperand(1)) >= 0;
                        case ArmConditionCode.ARM_CC_VS:
                            throw new NotImplementedException("Unimplemented cmp condition");
                        case ArmConditionCode.ARM_CC_VC:
                            throw new NotImplementedException("Unimplemented cmp condition");
                        case ArmConditionCode.ARM_CC_HI:
                            return GetOperand(0) > GetSecondOperand(1);
                        case ArmConditionCode.ARM_CC_LS:
                            return GetOperand(0) <= GetSecondOperand(1);
                        case ArmConditionCode.ARM_CC_GE:
                            return new CCast(new CType("int"), GetOperand(0)) >=
                                   new CCast(new CType("int"), GetSecondOperand(1));
                        case ArmConditionCode.ARM_CC_LT:
                            return new CCast(new CType("int"), GetOperand(0)) <
                                   new CCast(new CType("int"), GetSecondOperand(1));
                        case ArmConditionCode.ARM_CC_GT:
                            return new CCast(new CType("int"), GetOperand(0)) >
                                   new CCast(new CType("int"), GetSecondOperand(1));
                        case ArmConditionCode.ARM_CC_LE:
                            return new CCast(new CType("int"), GetOperand(0)) <=
                                   new CCast(new CType("int"), GetSecondOperand(1));
                        case ArmConditionCode.ARM_CC_AL:
                            return true;
                        default:
                            throw new ArgumentException("Invalid condition!");
                    }

                default:
                    throw new NotImplementedException("Unimplemented instruction!");
            }
        }
    }
}