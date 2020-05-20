using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gee.External.Capstone;
using Gee.External.Capstone.Arm;

namespace LibDerailer.CodeGraph
{
    public class Disassembler
    {
        public static Function DisassembleArm(byte[] data, uint dataAddress, ArmDisassembleMode mode)
        {
            var disassembler = CapstoneDisassembler.CreateArmDisassembler(mode);
            disassembler.DisassembleSyntax        = (DisassembleSyntax) 3;
            disassembler.EnableInstructionDetails = true;
            var disResult = disassembler.Disassemble(data, dataAddress);
            //check stack stuff
            var stackVars = new List<Variable>();
            int stackBase = 0;
            if (disResult[0].Id == ArmInstructionId.ARM_INS_PUSH)
            {
                stackBase = -disResult[0].Details.Operands.Length * 4;
                int offset = stackBase;
                foreach (var op in disResult[0].Details.Operands)
                {
                    stackVars.Add(new Variable(VariableLocation.Stack, "saved_" + op.Register.Name, offset, 4));
                    offset += 4;
                }
            }

            if (disResult[1].Id == ArmInstructionId.ARM_INS_SUB &&
                disResult[1].Details.Operands.Length == 3 &&
                disResult[1].Details.Operands[0].Type == ArmOperandType.Register &&
                disResult[1].Details.Operands[0].Register.Id == ArmRegisterId.ARM_REG_SP &&
                disResult[1].Details.Operands[1].Type == ArmOperandType.Register &&
                disResult[1].Details.Operands[1].Register.Id == ArmRegisterId.ARM_REG_SP &&
                disResult[1].Details.Operands[2].Type == ArmOperandType.Immediate)
            {
                stackBase -= disResult[1].Details.Operands[2].Immediate;
            }

            var func       = new Function(dataAddress);
            var boundaries = new Dictionary<uint, BasicBlock>();
            boundaries.Add(dataAddress, new BasicBlock(dataAddress));
            var queue = new Queue<int>();
            queue.Enqueue(0);
            while (queue.Count > 0)
            {
                int i        = queue.Dequeue();
                var curBlock = boundaries[(uint) disResult[i].Address];
                if (func.BasicBlocks.Contains(curBlock))
                    continue;
                while (true)
                {
                    var inst = disResult[i];
                    curBlock.Instructions.Add(new ArmMachineInstruction(inst));

                    if (inst.Details.BelongsToGroup(ArmInstructionGroupId.ARM_GRP_CALL) || (
                            !inst.Details.BelongsToGroup(ArmInstructionGroupId.ARM_GRP_BRANCH_RELATIVE) &&
                            !inst.Details.BelongsToGroup(ArmInstructionGroupId.ARM_GRP_JUMP) &&
                            !inst.Details.IsRegisterExplicitlyWritten(ArmRegisterId.ARM_REG_PC))) // &&
                        // inst.Details.ConditionCode == ArmConditionCode.ARM_CC_AL)
                    {
                        if (inst.Id == ArmInstructionId.ARM_INS_STR &&
                            inst.Details.Operands[1].Memory.Base.Id == ArmRegisterId.ARM_REG_SP)
                        {
                            int stackOffset = stackBase + inst.Details.Operands[1].Memory.Displacement;
                            if (!stackVars.Any(a => a.Address == stackOffset))
                            {
                                if (stackOffset < 0)
                                    stackVars.Add(new Variable(VariableLocation.Stack, $"stackVar_{-stackOffset:X02}",
                                        stackOffset, 4));
                                else
                                    stackVars.Add(new Variable(VariableLocation.Stack, $"arg_{stackOffset:X02}",
                                        stackOffset, 4));
                            }
                        }
                    }
                    else
                    {
                        if (inst.Details.Operands[0].Type == ArmOperandType.Immediate)
                        {
                            uint addr = (uint) inst.Details.Operands[0].Immediate;
                            if (!boundaries.ContainsKey(addr))
                            {
                                boundaries.Add(addr, new BasicBlock(addr));
                                queue.Enqueue(Array.FindIndex(disResult, a => a.Address == addr));
                            }

                            curBlock.Successors.Add(boundaries[addr]);
                            boundaries[addr].Predecessors.Add(curBlock);
                        }

                        if (inst.Details.ConditionCode != ArmConditionCode.ARM_CC_AL)
                        {
                            uint addr = (uint) disResult[i + 1].Address;
                            if (!boundaries.ContainsKey(addr))
                            {
                                boundaries.Add(addr, new BasicBlock(addr));
                                queue.Enqueue(i + 1);
                            }

                            curBlock.Successors.Add(boundaries[addr]);
                            boundaries[addr].Predecessors.Add(curBlock);
                        }

                        break;
                    }

                    i++;

                    if (boundaries.ContainsKey((uint) disResult[i].Address))
                    {
                        var next = boundaries[(uint) disResult[i].Address];
                        curBlock.Successors.Add(next);
                        next.Predecessors.Add(curBlock);
                        break;
                    }
                }

                func.BasicBlocks.Add(curBlock);
            }

            func.BasicBlocks.Sort();
            stackVars.Sort();
            return func;
        }
    }
}