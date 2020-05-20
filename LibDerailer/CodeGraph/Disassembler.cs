using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gee.External.Capstone;
using Gee.External.Capstone.Arm;
using LibDerailer.CodeGraph.Nodes;
using LibDerailer.IO;

namespace LibDerailer.CodeGraph
{
    public class Disassembler
    {
        private static void ResolveDefUseRelations(Function func)
        {
            foreach (var block in func.BasicBlocks)
            {
                for (int i = 0; i < block.Instructions.Count; i++)
                {
                    var inst = block.Instructions[i];
                    foreach (var use in inst.VariableUses)
                    {
                        bool done = false;
                        //check if there is a def before this instruction
                        for (int j = i - 1; j >= 0; j--)
                        {
                            if (block.Instructions[j].VariableDefs.Contains(use))
                            {
                                inst.VariableUseLocs[use] = new[] {block.Instructions[j]};
                                done                      = true;
                                break;
                            }
                        }

                        if (done)
                            continue;

                        //otherwise it's in one (or multiple) of the predecessors
                        var visited = new HashSet<BasicBlock>();
                        var stack   = new Stack<BasicBlock>(block.Predecessors);
                        var defs    = new List<Instruction>();
                        while (stack.Count > 0)
                        {
                            var block2 = stack.Pop();
                            if (visited.Contains(block2))
                                continue;
                            visited.Add(block2);
                            var def = block2.GetLastDef(use);
                            if (def != null)
                                defs.Add(def);
                            else
                            {
                                foreach (var pre in block2.Predecessors)
                                    stack.Push(pre);
                            }
                        }

                        inst.VariableUseLocs[use] = defs.ToArray();
                    }
                }
            }

            //debug: output dot
            File.WriteAllText(@"test.txt", func.DefUseGraphToDot());
        }

        public static Function DisassembleArm(byte[] data, uint dataAddress, ArmDisassembleMode mode)
        {
            var disassembler = CapstoneDisassembler.CreateArmDisassembler(mode);
            disassembler.DisassembleSyntax        = (DisassembleSyntax) 3;
            disassembler.EnableInstructionDetails = true;
            var disResult = disassembler.Disassemble(data, dataAddress);

            var regVars = new Variable[17];
            for (int i = 0; i < 16; i++)
                regVars[i] = new Variable(VariableLocation.Register, $"r{i}", i, 4);
            regVars[16] = new Variable(VariableLocation.Register, "cpsr", 16, 4);

            //Function prologue

            //Reg stores
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
            
            //Stack adjustment
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

            var func = new Function(dataAddress, stackBase);
            func.StackVariables.AddRange(stackVars);
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
                var blockCondition = curBlock.BlockCondition == ArmConditionCode.Invalid
                    ? ArmConditionCode.ARM_CC_AL
                    : curBlock.BlockCondition;
                while (true)
                {
                    var inst = disResult[i];

                    if (!inst.Details.BelongsToGroup(ArmInstructionGroupId.ARM_GRP_CALL) &&
                        (inst.Details.BelongsToGroup(ArmInstructionGroupId.ARM_GRP_BRANCH_RELATIVE) ||
                         inst.Details.BelongsToGroup(ArmInstructionGroupId.ARM_GRP_JUMP) ||
                         inst.Details.IsRegisterExplicitlyWritten(ArmRegisterId.ARM_REG_PC)))
                    {
                        //Branch
                        curBlock.Instructions.Add(new ArmMachineInstruction(inst, regVars));

                        if (inst.Id == ArmInstructionId.ARM_INS_BX &&
                            (inst.Details.ConditionCode == ArmConditionCode.ARM_CC_AL ||
                             inst.Details.ConditionCode == blockCondition))
                        {
                            break;
                        }

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
                    else if (inst.Details.ConditionCode != blockCondition)
                    {
                        //Conditional execution
                        //todo: support condition chains!!
                        uint addr = (uint) disResult[i].Address;
                        if (!boundaries.ContainsKey(addr))
                        {
                            boundaries.Add(addr, new BasicBlock(addr, inst.Details.ConditionCode));
                            queue.Enqueue(i);
                        }

                        curBlock.Successors.Add(boundaries[addr]);
                        boundaries[addr].Predecessors.Add(curBlock);

                        var oppCc = ArmUtil.GetOppositeCondition(inst.Details.ConditionCode);
                        //find the first instruction with opposite condition or no condition
                        int j = i + 1;
                        while (disResult[j].Details.ConditionCode != oppCc &&
                               disResult[j].Details.ConditionCode != ArmConditionCode.ARM_CC_AL)
                            j++;

                        addr = (uint) disResult[j].Address;
                        if (!boundaries.ContainsKey(addr))
                        {
                            boundaries.Add(addr, new BasicBlock(addr,
                                disResult[j].Details.ConditionCode == ArmConditionCode.ARM_CC_AL
                                    ? ArmConditionCode.Invalid
                                    : oppCc));
                            queue.Enqueue(j);
                        }

                        curBlock.Successors.Add(boundaries[addr]);
                        boundaries[addr].Predecessors.Add(curBlock);
                        break;
                    }
                    else if (inst.Id == ArmInstructionId.ARM_INS_LDR &&
                             inst.Details.Operands[1].Memory.Base.Id == ArmRegisterId.ARM_REG_PC)
                    {
                        //Constant pool load
                        curBlock.Instructions.Add(new LoadConstant(
                            regVars[ArmUtil.GetRegisterNumber(inst.Details.Operands[0].Register.Id)],
                            IOUtil.ReadU32Le(data, (int) (
                                inst.Address + inst.Details.Operands[1].Memory.Displacement + 8 - dataAddress))));
                    }
                    else if (inst.Details.BelongsToGroup(ArmInstructionGroupId.ARM_GRP_CALL))
                    {
                        //BL and BLX
                        curBlock.Instructions.Add(new ArmMachineInstruction(inst, regVars));
                    }
                    else
                    {
                        var graphInst = new ArmMachineInstruction(inst, regVars);
                        curBlock.Instructions.Add(graphInst);
                        if ((inst.Id == ArmInstructionId.ARM_INS_LDR || inst.Id == ArmInstructionId.ARM_INS_STR) &&
                            inst.Details.Operands[1].Memory.Base.Id == ArmRegisterId.ARM_REG_SP)
                        {
                            int stackOffset = stackBase + inst.Details.Operands[1].Memory.Displacement;
                            var stackVar    = func.StackVariables.FirstOrDefault(a => a.Address == stackOffset);
                            if (stackVar == null)
                            {
                                if (stackOffset < 0)
                                {
                                    stackVar = new Variable(VariableLocation.Stack,
                                        $"stackVar_{-stackOffset:X02}", stackOffset, 4);
                                    func.StackVariables.Add(stackVar);
                                }
                                else
                                {
                                    stackVar = new Variable(VariableLocation.Stack,
                                        $"arg_{stackOffset:X02}", stackOffset, 4);
                                    func.StackVariables.Add(stackVar);
                                }
                            }

                            if (inst.Id == ArmInstructionId.ARM_INS_LDR)
                                graphInst.VariableUses.Add(stackVar);
                            else
                                graphInst.VariableDefs.Add(stackVar);
                        }
                    }

                    i++;

                    //Stop at boundary
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
            func.StackVariables.Sort();
            //Add function entrance for arguments
            func.BasicBlocks[0].Instructions.Insert(0, new FunctionEntrance(func, regVars));
            ResolveDefUseRelations(func);
            return func;
        }
    }
}