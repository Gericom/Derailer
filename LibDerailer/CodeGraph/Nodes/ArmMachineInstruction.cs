﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Gee.External.Capstone;
using Gee.External.Capstone.Arm;
using LibDerailer.CCodeGen;
using LibDerailer.CCodeGen.Statements.Expressions;
using LibDerailer.IR;
using LibDerailer.IR.Expressions;
using LibDerailer.IR.Instructions;
using LibDerailer.IR.Types;

namespace LibDerailer.CodeGraph.Nodes
{
    public class ArmMachineInstruction : Instruction
    {
        public ArmInstruction Instruction { get; }

        private List<int> OpMapping { get; } = new List<int>();

        public ArmMachineInstruction(ArmInstruction instruction, Variable[] regVars)
            : base((uint) instruction.Address, instruction.Details.ConditionCode)
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
            if (instruction.Details.Operands.Length >= 3 &&
                instruction.Details.Operands[2].Type == ArmOperandType.Register &&
                instruction.Details.Operands[2].ShiftOperation >= ArmShiftOperation.ARM_SFT_LSL_REG)
                uses = uses.Append(
                    regVars[ArmUtil.GetRegisterNumber(instruction.Details.Operands[2].ShiftRegister.Id)]);
            VariableUses.UnionWith(uses);

            for (int i = 0; i < Instruction.Details.Operands.Length; i++)
            {
                var op = Instruction.Details.Operands[i];
                switch (op.Type)
                {
                    case ArmOperandType.Register:
                        if (Instruction.DisassembleMode == ArmDisassembleMode.Thumb &&
                            op.AccessType == (OperandAccessType.Read | OperandAccessType.Write) && i == 0)
                        {
                            Operands.Add((true, VariableDefs.FirstOrDefault(v =>
                                v.Location == VariableLocation.Register &&
                                v.Address == ArmUtil.GetRegisterNumber(op.Register.Id))));
                            OpMapping.Add(i);
                            Operands.Add((false, VariableUses.FirstOrDefault(v =>
                                v.Location == VariableLocation.Register &&
                                v.Address == ArmUtil.GetRegisterNumber(op.Register.Id))));
                            OpMapping.Add(i);
                        }
                        else if (op.AccessType == OperandAccessType.Write)
                        {
                            Operands.Add((true, VariableDefs.FirstOrDefault(v =>
                                v.Location == VariableLocation.Register &&
                                v.Address == ArmUtil.GetRegisterNumber(op.Register.Id))));
                            OpMapping.Add(i);
                        }
                        else
                        {
                            Operands.Add((false, VariableUses.FirstOrDefault(v =>
                                v.Location == VariableLocation.Register &&
                                v.Address == ArmUtil.GetRegisterNumber(op.Register.Id))));
                            OpMapping.Add(i);
                            if (op.ShiftOperation >= ArmShiftOperation.ARM_SFT_LSL_REG)
                            {
                                Operands.Add((false, VariableUses.First(v =>
                                    v.Location == VariableLocation.Register &&
                                    v.Address == ArmUtil.GetRegisterNumber(op.ShiftRegister.Id))));
                                OpMapping.Add(i);
                            }
                        }

                        break;
                    case ArmOperandType.Memory:
                        Operands.Add((false, VariableUses.First(v =>
                            v.Location == VariableLocation.Register &&
                            v.Address == ArmUtil.GetRegisterNumber(op.Memory.Base.Id))));
                        OpMapping.Add(i);
                        if (op.Memory.Index == null)
                            Operands.Add((false, new Constant((uint) op.Memory.Displacement)));
                        else
                            Operands.Add((false, VariableUses.First(v =>
                                v.Location == VariableLocation.Register &&
                                v.Address == ArmUtil.GetRegisterNumber(op.Memory.Index.Id))));

                        OpMapping.Add(i);
                        if (instruction.Details.WriteBack)
                        {
                            Operands.Add((true, VariableDefs.First(v =>
                                v.Location == VariableLocation.Register &&
                                v.Address == ArmUtil.GetRegisterNumber(op.Memory.Base.Id))));

                            OpMapping.Add(i);
                        }

                        break;
                    case ArmOperandType.Immediate:
                        Operands.Add((false, new Constant((uint) op.Immediate)));
                        OpMapping.Add(i);
                        break;
                    default:
                        Operands.Add((false, null));
                        OpMapping.Add(i);
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
            var op = Instruction.Details.Operands[OpMapping[idx]];
            if (op.Type == ArmOperandType.Immediate)
                return (uint) op.Immediate;

            var rm = context.VariableMapping[(Variable) Operands[idx].op];

            if (op.ShiftOperation == ArmShiftOperation.Invalid ||
                (op.ShiftOperation == ArmShiftOperation.ARM_SFT_LSL &&
                 op.ShiftValue == 0))
                return rm;

            IRVariable rs = null;
            if (op.ShiftOperation >= ArmShiftOperation.ARM_SFT_LSL_REG)
                rs = context.VariableMapping[(Variable) Operands[idx + 1].op];

            switch (op.ShiftOperation)
            {
                case ArmShiftOperation.ARM_SFT_LSL:
                    return rm.ShiftLeft(op.ShiftValue);
                case ArmShiftOperation.ARM_SFT_LSL_REG:
                    return rm.ShiftLeft(rs);
                case ArmShiftOperation.ARM_SFT_LSR:
                    return rm.ShiftRightLogical(op.ShiftValue);
                case ArmShiftOperation.ARM_SFT_LSR_REG:
                    return rm.ShiftRightLogical(rs);
                case ArmShiftOperation.ARM_SFT_ASR:
                    return rm.ShiftRightArithmetic(op.ShiftValue);
                case ArmShiftOperation.ARM_SFT_ASR_REG:
                    return rm.ShiftRightArithmetic(rs);
                default:
                    throw new NotImplementedException("Unimplemented shift!");
            }
        }

        public override IEnumerable<IRInstruction> GetIRInstructions(IRContext context, IRBasicBlock parentBlock)
        {
            bool isShortThumb = Instruction.DisassembleMode == ArmDisassembleMode.Thumb &&
                                Instruction.Details.Operands.Length == 2;
            switch (Instruction.Id)
            {
                case ArmInstructionId.ARM_INS_ADD:
                    if (Instruction.Details.Operands[0].Register.Id == ArmRegisterId.ARM_REG_PC)
                        yield break;
                    if (Instruction.DisassembleMode == ArmDisassembleMode.Thumb &&
                        Instruction.Details.Operands.Length == 3 &&
                        Instruction.Details.Operands[2].Type == ArmOperandType.Immediate &&
                        Instruction.Details.Operands[2].Immediate == 0)
                        yield return new IRAssignment(parentBlock, GetIROperand(context, 0),
                            GetIROperand(context, 1));
                    else
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
                    if (Instruction.DisassembleMode == ArmDisassembleMode.Arm)
                        goto case ArmInstructionId.ARM_INS_MOV;
                    yield return new IRAssignment(parentBlock, GetIROperand(context, 0),
                        GetIROperand(context, 1).ShiftLeft(GetIRSecondOperand(context, 2)));
                    break;
                case ArmInstructionId.ARM_INS_LSR:
                    if (Instruction.DisassembleMode == ArmDisassembleMode.Arm)
                        goto case ArmInstructionId.ARM_INS_MOV;
                    yield return new IRAssignment(parentBlock, GetIROperand(context, 0),
                        GetIROperand(context, 1).ShiftRightLogical(GetIRSecondOperand(context, 2)));
                    break;
                case ArmInstructionId.ARM_INS_ASR:
                    if (Instruction.DisassembleMode == ArmDisassembleMode.Arm)
                        goto case ArmInstructionId.ARM_INS_MOV;
                    yield return new IRAssignment(parentBlock, GetIROperand(context, 0),
                        GetIROperand(context, 1).ShiftRightArithmetic(GetIRSecondOperand(context, 2)));
                    break;
                case ArmInstructionId.ARM_INS_ROR:
                    if (Instruction.DisassembleMode == ArmDisassembleMode.Arm)
                        goto case ArmInstructionId.ARM_INS_MOV;
                    throw new NotImplementedException();
                    break;
                case ArmInstructionId.ARM_INS_RRX:
                    if (Instruction.DisassembleMode == ArmDisassembleMode.Arm)
                        goto case ArmInstructionId.ARM_INS_MOV;
                    throw new NotImplementedException();
                    break;
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
                case ArmInstructionId.ARM_INS_SMULL:
                    yield return new IRAssignment(parentBlock,
                        GetIROperand(context, 0),
                        (GetIROperand(context, 2).Cast(IRPrimitive.S32).Cast(IRPrimitive.S64) *
                         GetIROperand(context, 3).Cast(IRPrimitive.S32).Cast(IRPrimitive.S64)).Cast(IRPrimitive.U32));
                    yield return new IRAssignment(parentBlock,
                        GetIROperand(context, 1),
                        (GetIROperand(context, 2).Cast(IRPrimitive.S32).Cast(IRPrimitive.S64) *
                         GetIROperand(context, 3).Cast(IRPrimitive.S32).Cast(IRPrimitive.S64)).ShiftRightLogical(32)
                        .Cast(IRPrimitive.U32));
                    break;
                case ArmInstructionId.ARM_INS_UMULL:
                    yield return new IRAssignment(parentBlock,
                        GetIROperand(context, 0),
                        (GetIROperand(context, 2).Cast(IRPrimitive.U32).Cast(IRPrimitive.U64) *
                         GetIROperand(context, 3).Cast(IRPrimitive.U32).Cast(IRPrimitive.U64)).Cast(IRPrimitive.U32));
                    yield return new IRAssignment(parentBlock,
                        GetIROperand(context, 1),
                        (GetIROperand(context, 2).Cast(IRPrimitive.U32).Cast(IRPrimitive.U64) *
                         GetIROperand(context, 3).Cast(IRPrimitive.U32).Cast(IRPrimitive.U64)).ShiftRightLogical(32)
                        .Cast(IRPrimitive.U32));
                    break;
                case ArmInstructionId.ARM_INS_LDR:
                case ArmInstructionId.ARM_INS_LDRH:
                case ArmInstructionId.ARM_INS_LDRSH:
                case ArmInstructionId.ARM_INS_LDRB:
                case ArmInstructionId.ARM_INS_LDRSB:
                {
                    IRType type = IRPrimitive.Void;
                    switch (Instruction.Id)
                    {
                        case ArmInstructionId.ARM_INS_LDR:
                            type = IRPrimitive.U32;
                            break;
                        case ArmInstructionId.ARM_INS_LDRH:
                            type = IRPrimitive.U16;
                            break;
                        case ArmInstructionId.ARM_INS_LDRSH:
                            type = IRPrimitive.S16;
                            break;
                        case ArmInstructionId.ARM_INS_LDRB:
                            type = IRPrimitive.U8;
                            break;
                        case ArmInstructionId.ARM_INS_LDRSB:
                            type = IRPrimitive.S8;
                            break;
                    }

                    if (Instruction.Details.WriteBack)
                        throw new NotImplementedException("Unimplemented instruction!");


                    if (Instruction.Details.Operands[1].Memory.Base.Id == ArmRegisterId.ARM_REG_SP)
                    {
                        yield return new IRAssignment(parentBlock, GetIROperand(context, 0),
                            context.VariableMapping[VariableUses.First(v => v.Location == VariableLocation.Stack)]);
                        break;
                    }

                    if (Instruction.Details.Operands[1].Memory.Index == null &&
                        Instruction.Details.Operands[1].Memory.Displacement == 0)
                    {
                        yield return new IRAssignment(parentBlock, GetIROperand(context, 0),
                            new IRDerefExpression(type, GetIROperand(context, 1).Cast(type.GetPointer())).Cast(
                                IRPrimitive.U32));
                        break;
                    }
                    else if (Instruction.Details.Operands[1].Memory.Index == null)
                    {
                        var deref = new IRDerefExpression(type,
                            (GetIROperand(context, 1) + (uint) Instruction.Details.Operands[1].Memory.Displacement)
                            .Cast(type.GetPointer()));
                        yield return new IRAssignment(parentBlock, GetIROperand(context, 0),
                            deref.Cast(IRPrimitive.U32));
                        break;
                    }
                    else
                    {
                        if (Instruction.Details.Operands[1].ShiftOperation == ArmShiftOperation.Invalid ||
                            (Instruction.Details.Operands[1].ShiftOperation == ArmShiftOperation.ARM_SFT_LSL &&
                             Instruction.Details.Operands[1].ShiftValue == 0))
                        {
                            var deref = new IRDerefExpression(type,
                                (GetIROperand(context, 1) + GetIROperand(context, 2)).Cast(type.GetPointer()));
                            yield return new IRAssignment(parentBlock, GetIROperand(context, 0),
                                deref.Cast(IRPrimitive.U32));
                            break;
                        }
                        else
                        {
                            IRExpression disp = GetIROperand(context, 2);
                            switch (Instruction.Details.Operands[1].ShiftOperation)
                            {
                                case ArmShiftOperation.ARM_SFT_LSL:
                                    disp = disp.ShiftLeft(Instruction.Details.Operands[1].ShiftValue);
                                    break;
                                case ArmShiftOperation.ARM_SFT_LSR:
                                    disp = disp.ShiftRightLogical(Instruction.Details.Operands[1].ShiftValue);
                                    break;
                                case ArmShiftOperation.ARM_SFT_ASR:
                                    disp = disp.ShiftRightArithmetic(Instruction.Details.Operands[1].ShiftValue);
                                    break;
                                default:
                                    throw new NotImplementedException("Unimplemented shift!");
                            }

                            var deref = new IRDerefExpression(type,
                                (GetIROperand(context, 1) + disp).Cast(type.GetPointer()));
                            yield return new IRAssignment(parentBlock, GetIROperand(context, 0),
                                deref.Cast(IRPrimitive.U32));
                            break;
                        }
                    }
                }
                case ArmInstructionId.ARM_INS_STR:
                case ArmInstructionId.ARM_INS_STRH:
                case ArmInstructionId.ARM_INS_STRB:
                {
                    var type = IRPrimitive.Void;
                    switch (Instruction.Id)
                    {
                        case ArmInstructionId.ARM_INS_STR:
                            type = IRPrimitive.U32;
                            break;
                        case ArmInstructionId.ARM_INS_STRH:
                            type = IRPrimitive.U16;
                            break;
                        case ArmInstructionId.ARM_INS_STRB:
                            type = IRPrimitive.U8;
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
                        yield return new IRAssignment(parentBlock,
                            new IRDerefExpression(type, GetIROperand(context, 1).Cast(type.GetPointer())),
                            GetIROperand(context, 0).Cast(type));
                        break;
                    }
                    else if (Instruction.Details.Operands[1].Memory.Index == null)
                    {
                        yield return new IRAssignment(parentBlock,
                            new IRDerefExpression(type,
                                (GetIROperand(context, 1) + (uint) Instruction.Details.Operands[1].Memory.Displacement)
                                .Cast(type.GetPointer())),
                            GetIROperand(context, 0).Cast(type));
                        break;
                    }
                    else
                    {
                        if (Instruction.Details.Operands[1].ShiftOperation == ArmShiftOperation.Invalid ||
                            (Instruction.Details.Operands[1].ShiftOperation == ArmShiftOperation.ARM_SFT_LSL &&
                             Instruction.Details.Operands[1].ShiftValue == 0))
                        {
                            yield return new IRAssignment(parentBlock,
                                new IRDerefExpression(type,
                                    (GetIROperand(context, 1) + GetIROperand(context, 2)).Cast(type.GetPointer())),
                                GetIROperand(context, 0).Cast(type));
                            break;
                        }
                        else
                        {
                            IRExpression disp = GetIROperand(context, 2);
                            switch (Instruction.Details.Operands[1].ShiftOperation)
                            {
                                case ArmShiftOperation.ARM_SFT_LSL:
                                    disp = disp.ShiftLeft(Instruction.Details.Operands[1].ShiftValue);
                                    break;
                                case ArmShiftOperation.ARM_SFT_LSR:
                                    disp = disp.ShiftRightLogical(Instruction.Details.Operands[1].ShiftValue);
                                    break;
                                case ArmShiftOperation.ARM_SFT_ASR:
                                    disp = disp.ShiftRightArithmetic(Instruction.Details.Operands[1].ShiftValue);
                                    break;
                                default:
                                    throw new NotImplementedException("Unimplemented shift!");
                            }

                            yield return new IRAssignment(parentBlock,
                                new IRDerefExpression(type,
                                    (GetIROperand(context, 1) + disp).Cast(type.GetPointer())),
                                GetIROperand(context, 0).Cast(type));
                            break;
                        }
                    }
                }
                case ArmInstructionId.ARM_INS_BL:
                {
                    string symbol =
                        context.ProgramContext?.TryGetSymbol((uint) Instruction.Details.Operands[0].Immediate) ??
                        $"sub_{Instruction.Details.Operands[0].Immediate:X08}";
                    if (symbol == "_s32_div_f")
                    {
                        yield return new IRAssignment(parentBlock, context.VariableMapping[VariableDefs
                                .First(v => v.Location == VariableLocation.Register && v.Address == 0)],
                            (context.VariableMapping[VariableUses
                                     .First(v => v.Location == VariableLocation.Register && v.Address == 0)]
                                 .Cast(IRPrimitive.S32) /
                             context.VariableMapping[VariableUses
                                     .First(v => v.Location == VariableLocation.Register && v.Address == 1)]
                                 .Cast(IRPrimitive.S32)).Cast(IRPrimitive.U32));
                        yield return new IRAssignment(parentBlock, context.VariableMapping[VariableDefs
                                .First(v => v.Location == VariableLocation.Register && v.Address == 1)],
                            (context.VariableMapping[VariableUses
                                     .First(v => v.Location == VariableLocation.Register && v.Address == 0)]
                                 .Cast(IRPrimitive.S32) %
                             context.VariableMapping[VariableUses
                                     .First(v => v.Location == VariableLocation.Register && v.Address == 1)]
                                 .Cast(IRPrimitive.S32)).Cast(IRPrimitive.U32));
                    }
                    else if (symbol == "_ll_mul")
                    {
                        var opA = context.VariableMapping[VariableUses
                                          .First(v => v.Location == VariableLocation.Register && v.Address == 0)]
                                      .Cast(IRPrimitive.U32).Cast(IRPrimitive.S64) |
                                  context.VariableMapping[VariableUses
                                          .First(v => v.Location == VariableLocation.Register && v.Address == 1)]
                                      .Cast(IRPrimitive.U32).Cast(IRPrimitive.S64).ShiftLeft(32);

                        var opB = context.VariableMapping[VariableUses
                                          .First(v => v.Location == VariableLocation.Register && v.Address == 2)]
                                      .Cast(IRPrimitive.U32).Cast(IRPrimitive.S64) |
                                  context.VariableMapping[VariableUses
                                          .First(v => v.Location == VariableLocation.Register && v.Address == 3)]
                                      .Cast(IRPrimitive.U32).Cast(IRPrimitive.S64).ShiftLeft(32);

                        var mulResult = opA * opB;

                        yield return new IRAssignment(parentBlock, context.VariableMapping[VariableDefs
                                .First(v => v.Location == VariableLocation.Register && v.Address == 0)],
                            mulResult.Cast(IRPrimitive.U32));
                        yield return new IRAssignment(parentBlock, context.VariableMapping[VariableDefs
                                .First(v => v.Location == VariableLocation.Register && v.Address == 1)],
                            mulResult.ShiftRightArithmetic(32).Cast(IRPrimitive.U32));
                    }
                    else
                    {
                        yield return new IRAssignment(parentBlock,
                            context.VariableMapping[VariableDefs
                                .First(v => v.Location == VariableLocation.Register && v.Address == 0)],
                            new IRCallExpression(IRPrimitive.U32, symbol,
                                context.VariableMapping[VariableUses
                                    .First(v => v.Location == VariableLocation.Register && v.Address == 0)],
                                context.VariableMapping[VariableUses
                                    .First(v => v.Location == VariableLocation.Register && v.Address == 1)],
                                context.VariableMapping[VariableUses
                                    .First(v => v.Location == VariableLocation.Register && v.Address == 2)],
                                context.VariableMapping[VariableUses
                                    .First(v => v.Location == VariableLocation.Register && v.Address == 3)]
                            ));
                    }

                    break;
                }
                case ArmInstructionId.ARM_INS_BLX:
                    if (Instruction.Details.Operands[0].Type == ArmOperandType.Immediate)
                        goto case ArmInstructionId.ARM_INS_BL;
                    yield return new IRAssignment(parentBlock,
                        context.VariableMapping[VariableDefs
                            .First(v => v.Location == VariableLocation.Register && v.Address == 0)],
                        new IRCallExpression(IRPrimitive.U32, ((Variable) Operands[0].op).Name,
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
                    break;
                //goto default;
                case ArmInstructionId.ARM_INS_STM:
                    break;
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
                    throw new NotImplementedException(
                        $"Unimplemented instruction: \"{Instruction.Mnemonic} {Instruction.Operand}\"");
            }
        }

        public override IRExpression GetIRPredicateCode(IRContext context, ArmConditionCode condition)
        {
            switch (Instruction.Id)
            {
                case ArmInstructionId.ARM_INS_AND:
                case ArmInstructionId.ARM_INS_LSR:
                case ArmInstructionId.ARM_INS_ASR:
                case ArmInstructionId.ARM_INS_LSL:
                case ArmInstructionId.ARM_INS_MOV:
                    switch (condition)
                    {
                        case ArmConditionCode.ARM_CC_EQ:
                            return GetIROperand(context, 0).Cast(IRPrimitive.U32) == 0u;
                        case ArmConditionCode.ARM_CC_NE:
                            return GetIROperand(context, 0).Cast(IRPrimitive.U32) != 0u;
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
                            return GetIROperand(context, 0).Cast(IRPrimitive.U32)
                                .UnsignedGreaterEqualAs(GetIRSecondOperand(context, 1).Cast(IRPrimitive.U32));
                        case ArmConditionCode.ARM_CC_LO:
                            return GetIROperand(context, 0).Cast(IRPrimitive.U32)
                                .UnsignedLessThan(GetIRSecondOperand(context, 1).Cast(IRPrimitive.U32));
                        case ArmConditionCode.ARM_CC_MI:
                            return (GetIROperand(context, 0) - GetIRSecondOperand(context, 1)).LessThan(0);
                        case ArmConditionCode.ARM_CC_PL:
                            return (GetIROperand(context, 0) - GetIRSecondOperand(context, 1)).GreaterThan(0);
                        case ArmConditionCode.ARM_CC_VS:
                            throw new NotImplementedException("Unimplemented cmp condition");
                        case ArmConditionCode.ARM_CC_VC:
                            throw new NotImplementedException("Unimplemented cmp condition");
                        case ArmConditionCode.ARM_CC_HI:
                            return GetIROperand(context, 0).Cast(IRPrimitive.U32)
                                .UnsignedGreaterThan(GetIRSecondOperand(context, 1).Cast(IRPrimitive.U32));
                        case ArmConditionCode.ARM_CC_LS:
                            return GetIROperand(context, 0).Cast(IRPrimitive.U32)
                                .UnsignedLessEqualAs(GetIRSecondOperand(context, 1).Cast(IRPrimitive.U32));
                        case ArmConditionCode.ARM_CC_GE:
                            return GetIROperand(context, 0).Cast(IRPrimitive.S32)
                                .GreaterEqualAs(GetIRSecondOperand(context, 1).Cast(IRPrimitive.S32));
                        case ArmConditionCode.ARM_CC_LT:
                            return GetIROperand(context, 0).Cast(IRPrimitive.S32)
                                .LessThan(GetIRSecondOperand(context, 1).Cast(IRPrimitive.S32));
                        case ArmConditionCode.ARM_CC_GT:
                            return GetIROperand(context, 0).Cast(IRPrimitive.S32)
                                .GreaterThan(GetIRSecondOperand(context, 1).Cast(IRPrimitive.S32));
                        case ArmConditionCode.ARM_CC_LE:
                            return GetIROperand(context, 0).Cast(IRPrimitive.S32)
                                .LessEqualAs(GetIRSecondOperand(context, 1).Cast(IRPrimitive.S32));
                        case ArmConditionCode.ARM_CC_AL:
                            return true;
                        default:
                            throw new ArgumentException("Invalid condition!");
                    }

                default:
                    throw new NotImplementedException(
                        $"Unimplemented instruction: \"{Instruction.Mnemonic} {Instruction.Operand}\"");
            }
        }
    }
}