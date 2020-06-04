using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gee.External.Capstone;
using Gee.External.Capstone.Arm;
using LibDerailer.Analysis;
using LibDerailer.CCodeGen;
using LibDerailer.CCodeGen.Statements;
using LibDerailer.CCodeGen.Statements.Expressions;
using LibDerailer.CodeGraph;
using LibDerailer.CodeGraph.Nodes;
using LibDerailer.IO;
using LibDerailer.IR;
using LibDerailer.IR.Expressions;
using LibDerailer.IR.Instructions;
using LibDerailer.IR.Types;

namespace LibDerailer.CodeGraph
{
    public partial class Disassembler
    {
        private static void ResolveDefUseRelations(Function func)
        {
            var dict = new Dictionary<(Instruction, Variable), HashSet<Instruction>>();
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
                                inst.VariableUseLocs[use] = new HashSet<Instruction>() {block.Instructions[j]};
                                if (!dict.ContainsKey((block.Instructions[j], use)))
                                    dict[(block.Instructions[j], use)] = new HashSet<Instruction>();
                                dict[(block.Instructions[j], use)].Add(inst);
                                done = true;
                                break;
                            }
                        }

                        if (done)
                            continue;

                        //otherwise it's in one (or multiple) of the predecessors
                        var visited = new HashSet<BasicBlock>();
                        var stack   = new Stack<BasicBlock>(block.Predecessors);
                        var defs    = new HashSet<Instruction>();
                        while (stack.Count > 0)
                        {
                            var block2 = stack.Pop();
                            if (visited.Contains(block2))
                                continue;
                            visited.Add(block2);
                            var def = block2.GetLastDef(use);
                            if (def != null)
                            {
                                defs.Add(def);
                                if (!dict.ContainsKey((def, use)))
                                    dict[(def, use)] = new HashSet<Instruction>();
                                dict[(def, use)].Add(inst);
                            }
                            else
                            {
                                foreach (var pre in block2.Predecessors)
                                    stack.Push(pre);
                            }
                        }

                        inst.VariableUseLocs[use] = defs;
                    }
                }

                if (block.BlockCondition != ArmConditionCode.Invalid)
                    block.BlockConditionInstruction =
                        block.Instructions[0].VariableUseLocs[func.MachineRegisterVariables[16]].First();
            }

            foreach (var (inst, var) in dict.Keys)
                inst.VariableDefLocs[var] = dict[(inst, var)];
        }

        public static Function DisassembleArm(byte[] data, uint dataAddress, ArmDisassembleMode mode)
        {
            bool hasReturnValue = true;

            var disassembler = CapstoneDisassembler.CreateArmDisassembler(mode);
            disassembler.DisassembleSyntax        = (DisassembleSyntax) 3;
            disassembler.EnableInstructionDetails = true;
            disassembler.EnableSkipDataMode       = true;
            var disResult = disassembler.Disassemble(data, dataAddress);

            //Function prologue

            //Reg stores
            var savedRegs = new Variable[16];
            var stackVars = new List<Variable>();
            int stackBase = 0;
            if (disResult[0].Id == ArmInstructionId.ARM_INS_PUSH ||
                (disResult[0].Id == ArmInstructionId.ARM_INS_STMDB &&
                 disResult[0].Details.Operands[0].Register.Id == ArmRegisterId.ARM_REG_SP &&
                 disResult[0].Details.WriteBack))
            {
                stackBase = -disResult[0].Details.Operands.Length * 4;
                int offset = stackBase;
                foreach (var op in disResult[0].Details.Operands)
                {
                    var savVar = new Variable(VariableLocation.Stack, "saved_" + op.Register.Name, offset, 4);
                    savedRegs[ArmUtil.GetRegisterNumber(op.Register.Id)] = savVar;
                    stackVars.Add(savVar);
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

            //function epilogue
            BasicBlock epilogue = null;
            for (int i = disResult.Length - 1; i >= 0; i--)
            {
                var inst = disResult[i];
                if (inst.Id == ArmInstructionId.ARM_INS_BX &&
                    inst.Details.ConditionCode == ArmConditionCode.ARM_CC_AL)
                {
                    //found last bx lr
                    //check for reg restore and stack adjustment
                    if (i - 1 >= 0)
                    {
                        var inst2 = disResult[i - 1];
                        if (inst2.Id == ArmInstructionId.ARM_INS_POP ||
                            (inst2.Id == ArmInstructionId.ARM_INS_LDM &&
                             inst2.Details.Operands[0].Register.Id == ArmRegisterId.ARM_REG_SP))
                        {
                            i--;
                        }
                    }

                    if (i - 1 >= 0)
                    {
                        var inst2 = disResult[i - 1];
                        if (inst2.Id == ArmInstructionId.ARM_INS_ADD &&
                            inst2.Details.Operands[0].Register.Id == ArmRegisterId.ARM_REG_SP &&
                            inst2.Details.Operands[1].Register.Id == ArmRegisterId.ARM_REG_SP &&
                            inst2.Details.Operands[2].Type == ArmOperandType.Immediate)
                        {
                            i--;
                        }
                    }

                    epilogue = new BasicBlock((uint) disResult[i].Address);
                    boundaries.Add((uint) disResult[i].Address, epilogue);
                    queue.Enqueue(i);

                    break;
                }
            }

            func.Epilogue = epilogue;

            while (queue.Count > 0)
            {
                int i        = queue.Dequeue();
                var curBlock = boundaries[(uint) disResult[i].Address];
                var blockCondition = curBlock.BlockCondition == ArmConditionCode.Invalid
                    ? ArmConditionCode.ARM_CC_AL
                    : curBlock.BlockCondition;
                while (true)
                {
                    var inst = disResult[i];

                    if (ArmUtil.IsJump(inst))
                    {
                        //Branch
                        if (inst.Id == ArmInstructionId.ARM_INS_ADD &&
                            inst.Details.ConditionCode == ArmConditionCode.ARM_CC_LS &&
                            inst.Details.Operands[0].Register.Id == ArmRegisterId.ARM_REG_PC &&
                            inst.Details.Operands[1].Register.Id == ArmRegisterId.ARM_REG_PC &&
                            disResult[i - 1].Id == ArmInstructionId.ARM_INS_CMP)
                        {
                            //This is a switch
                            int caseCount = disResult[i - 1].Details.Operands[1].Immediate + 1;
                            for (int j = -1; j < caseCount; j++)
                            {
                                uint addr = (uint) (inst.Address + 8 + (j << inst.Details.Operands[2].ShiftValue));
                                if (!boundaries.ContainsKey(addr))
                                {
                                    boundaries.Add(addr, new BasicBlock(addr));
                                    queue.Enqueue(Array.FindIndex(disResult, a => a.Address == addr));
                                }
                            }

                            break;
                        }

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
                        }

                        if (inst.Details.ConditionCode != ArmConditionCode.ARM_CC_AL)
                        {
                            uint addr = (uint) disResult[i + 1].Address;
                            if (!boundaries.ContainsKey(addr))
                            {
                                boundaries.Add(addr, new BasicBlock(addr));
                                queue.Enqueue(i + 1);
                            }
                        }

                        break;
                    }
                    else if (inst.Details.ConditionCode != blockCondition)
                    {
                        uint addr;
                        if (inst.Details.ConditionCode == ArmConditionCode.ARM_CC_AL)
                        {
                            addr = (uint) disResult[i].Address;
                            if (!boundaries.ContainsKey(addr))
                            {
                                boundaries.Add(addr, new BasicBlock(addr, inst.Details.ConditionCode));
                                queue.Enqueue(i);
                            }

                            break;
                        }

                        //Conditional execution
                        //todo: support condition chains!!
                        addr = (uint) disResult[i].Address;
                        if (!boundaries.ContainsKey(addr))
                        {
                            boundaries.Add(addr, new BasicBlock(addr, inst.Details.ConditionCode));
                            queue.Enqueue(i);
                        }

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

                        break;
                    }

                    i++;

                    //Stop at boundary
                    if (boundaries.ContainsKey((uint) disResult[i].Address))
                    {
                        var next = boundaries[(uint) disResult[i].Address];
                        break;
                    }
                }
            }

            queue.Enqueue(0);
            foreach (var boundary in boundaries)
                queue.Enqueue(Array.FindIndex(disResult, a => a.Address == boundary.Value.Address));
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

                    if (ArmUtil.IsJump(inst))
                    {
                        //Branch
                        var instruction = new ArmMachineInstruction(inst, func.MachineRegisterVariables);

                        if (inst.Id == ArmInstructionId.ARM_INS_ADD &&
                            inst.Details.ConditionCode == ArmConditionCode.ARM_CC_LS &&
                            inst.Details.Operands[0].Register.Id == ArmRegisterId.ARM_REG_PC &&
                            inst.Details.Operands[1].Register.Id == ArmRegisterId.ARM_REG_PC &&
                            disResult[i - 1].Id == ArmInstructionId.ARM_INS_CMP)
                        {
                            //This is a switch
                            int caseCount = disResult[i - 1].Details.Operands[1].Immediate + 1;
                            for (int j = -1; j < caseCount; j++)
                            {
                                uint addr = (uint) (inst.Address + 8 + (j << inst.Details.Operands[2].ShiftValue));
                                if (!boundaries.ContainsKey(addr))
                                {
                                    boundaries.Add(addr, new BasicBlock(addr));
                                    queue.Enqueue(Array.FindIndex(disResult, a => a.Address == addr));
                                }

                                curBlock.Successors.Add(boundaries[addr]);
                                boundaries[addr].Predecessors.Add(curBlock);
                            }

                            curBlock.Instructions.Add(instruction);

                            break;
                        }

                        if (inst.Id == ArmInstructionId.ARM_INS_BX &&
                            (inst.Details.ConditionCode == ArmConditionCode.ARM_CC_AL ||
                             inst.Details.ConditionCode == blockCondition))
                        {
                            if (stackBase == 0 && inst.Details.Operands[0].Register.Id == ArmRegisterId.ARM_REG_LR)
                            {
                                instruction.VariableUses.Remove(func.MachineRegisterVariables[2]);
                                instruction.VariableUses.Remove(func.MachineRegisterVariables[3]);
                            }

                            curBlock.Instructions.Add(instruction);

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

                            curBlock.BlockBranch = new Branch(inst.Details.ConditionCode, boundaries[addr],
                                func.MachineRegisterVariables[16]);
                            curBlock.Instructions.Add(curBlock.BlockBranch);
                        }
                        else
                            curBlock.Instructions.Add(instruction);

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
                        uint addr;
                        if (inst.Details.ConditionCode == ArmConditionCode.ARM_CC_AL)
                        {
                            addr = (uint) disResult[i].Address;
                            if (!boundaries.ContainsKey(addr))
                            {
                                boundaries.Add(addr, new BasicBlock(addr, inst.Details.ConditionCode));
                                queue.Enqueue(i);
                            }

                            curBlock.Successors.Add(boundaries[addr]);
                            boundaries[addr].Predecessors.Add(curBlock);
                            break;
                        }

                        //Conditional execution
                        //todo: support condition chains!!
                        addr = (uint) disResult[i].Address;
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
                            inst.Details.ConditionCode,
                            func.MachineRegisterVariables[
                                ArmUtil.GetRegisterNumber(inst.Details.Operands[0].Register.Id)],
                            IOUtil.ReadU32Le(data, (int) (
                                inst.Address + inst.Details.Operands[1].Memory.Displacement + 8 - dataAddress))));
                    }
                    else if (inst.Details.BelongsToGroup(ArmInstructionGroupId.ARM_GRP_CALL))
                    {
                        //BL and BLX
                        curBlock.Instructions.Add(new ArmMachineInstruction(inst, func.MachineRegisterVariables));
                    }
                    else
                    {
                        var graphInst = new ArmMachineInstruction(inst, func.MachineRegisterVariables);
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

                        if (inst.Id == ArmInstructionId.ARM_INS_PUSH ||
                            (inst.Id == ArmInstructionId.ARM_INS_STMDB &&
                             inst.Details.Operands[0].Register.Id == ArmRegisterId.ARM_REG_SP &&
                             inst.Details.WriteBack))
                        {
                            foreach (var op in inst.Details.Operands)
                                graphInst.VariableDefs.Add(stackVars.First(v => v.Name == "saved_" + op.Register.Name));
                        }
                        else if (inst.Id == ArmInstructionId.ARM_INS_POP ||
                                 (inst.Id == ArmInstructionId.ARM_INS_LDM &&
                                  inst.Details.Operands[0].Register.Id == ArmRegisterId.ARM_REG_SP &&
                                  inst.Details.WriteBack))
                        {
                            foreach (var op in inst.Details.Operands)
                                graphInst.VariableUses.Add(stackVars.First(v => v.Name == "saved_" + op.Register.Name));
                        }
                    }

                    i++;

                    //Stop at boundary
                    if (boundaries.ContainsKey((uint) disResult[i].Address) &&
                        (blockCondition != ArmConditionCode.ARM_CC_AL ||
                         disResult[i].Details.ConditionCode == blockCondition))
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
            var funcEntrance = new FunctionEntrance(func, func.MachineRegisterVariables);
            func.BasicBlocks[0].Instructions.Insert(0, funcEntrance);
            ResolveDefUseRelations(func);
            FlattenConditionals(func);
            DescheduleConditionals(func);

            int orderIndex = 0;
            foreach (var block in func.BasicBlocks)
                foreach (var instruction in block.Instructions)
                    instruction.OrderIndex = orderIndex++;

            RecreateConditionalBasicBlocks(func);

            func.BasicBlocks.Sort();

            //replace epilogues with branches to the final epilogue
            var initialLR = funcEntrance.VariableDefs.First(v =>
                v.Location == VariableLocation.Register && v.Address == 14);
            var returnBfsQueue = new Queue<Instruction>();
            var returnVisited  = new HashSet<Instruction>();
            var returns        = new List<Instruction>();
            foreach (var lrUse in funcEntrance.VariableDefLocs[initialLR])
                returnBfsQueue.Enqueue(lrUse);
            while (returnBfsQueue.Count > 0)
            {
                var inst = returnBfsQueue.Dequeue();
                if (returnVisited.Contains(inst))
                    continue;
                returnVisited.Add(inst);
                if (!(inst is ArmMachineInstruction mi))
                    continue; //this shouldn't happen
                if (mi.Instruction.Id == ArmInstructionId.ARM_INS_BX)
                {
                    returns.Add(mi);
                }
                else if (mi.Instruction.Id == ArmInstructionId.ARM_INS_PUSH ||
                         (mi.Instruction.Id == ArmInstructionId.ARM_INS_STMDB &&
                          mi.Instruction.Details.Operands[0].Register.Id == ArmRegisterId.ARM_REG_SP))
                {
                    foreach (var lrUse in mi.VariableDefLocs[savedRegs[14]])
                        returnBfsQueue.Enqueue(lrUse);
                }
                else if (mi.Instruction.Id == ArmInstructionId.ARM_INS_POP ||
                         (mi.Instruction.Id == ArmInstructionId.ARM_INS_LDM &&
                          mi.Instruction.Details.Operands[0].Register.Id == ArmRegisterId.ARM_REG_SP))
                {
                    foreach (var lrUse in mi.VariableDefLocs[mi.VariableDefs.First(
                        v => v.Location == VariableLocation.Register && v.Address == 14)])
                        returnBfsQueue.Enqueue(lrUse);
                }
            }

            foreach (var ret in returns)
            {
                var block = func.BasicBlocks.First(b => b.Instructions.Contains(ret));
                if (block == epilogue)
                    continue;
                //find all up to three epilogue instructions in the current block
                var blockEpInsts = new List<Instruction>();
                int i            = 0;
                foreach (var epInst in epilogue.Instructions)
                {
                    var epInstArm = (ArmMachineInstruction) epInst;
                    while (i < block.Instructions.Count)
                    {
                        if (!(block.Instructions[i++] is ArmMachineInstruction im))
                            continue;
                        if (im.Instruction.Id != epInstArm.Instruction.Id)
                            continue;
                        if (im.Instruction.Details.Operands[0].Register.Id !=
                            epInstArm.Instruction.Details.Operands[0].Register.Id)
                            continue;
                        blockEpInsts.Add(im);
                        break;
                    }

                    if (i == block.Instructions.Count)
                        break;
                }

                if (blockEpInsts.Count != epilogue.Instructions.Count)
                    throw new Exception("Invalid epilogue");
                foreach (var epInst in blockEpInsts)
                {
                    block.Instructions.Remove(epInst);
                    foreach (var use in epInst.VariableUses)
                        foreach (var useLoc in epInst.VariableUseLocs[use])
                            useLoc.VariableDefLocs[use].Remove(epInst);
                }

                if (block.Instructions.Count == 0)
                {
                    //replace predecessor branches with epilogue branches
                    foreach (var predecessor in block.Predecessors)
                    {
                        predecessor.Successors.Remove(block);
                        predecessor.Successors.Add(epilogue);
                        epilogue.Predecessors.Add(predecessor);
                        if (predecessor.BlockBranch != null && predecessor.BlockBranch.Destination == block)
                            predecessor.BlockBranch.Destination = epilogue;
                    }

                    func.BasicBlocks.Remove(block);
                }
                else
                {
                    var branch = new Branch(
                        block.BlockCondition == ArmConditionCode.Invalid
                            ? blockEpInsts.Last().Condition
                            : block.BlockCondition,
                        epilogue,
                        blockEpInsts[0].FlagsUseOperand as Variable);
                    if (branch.Condition != ArmConditionCode.ARM_CC_AL)
                        branch.VariableUseLocs[(Variable) blockEpInsts[0].FlagsUseOperand] = blockEpInsts[0]
                            .VariableUseLocs[(Variable) blockEpInsts[0].FlagsUseOperand];
                    block.BlockBranch = branch;
                    block.Instructions.Add(branch);
                    block.Successors.Add(epilogue);
                    epilogue.Predecessors.Add(block);
                }
            }

            if (epilogue != null)
            {
                FunctionExit funcExit;
                if (hasReturnValue)
                {
                    funcExit = new FunctionExit(func.MachineRegisterVariables[0]);
                    funcExit.VariableUseLocs[func.MachineRegisterVariables[0]] = epilogue.Instructions.Last()
                        .VariableUseLocs[func.MachineRegisterVariables[0]];
                }
                else
                    funcExit = new FunctionExit(null);

                epilogue.Instructions.Clear();
                epilogue.Instructions.Add(funcExit);
                epilogue.Instructions[0].OrderIndex = orderIndex++;
            }

            ResolveDefUseRelations(func);


            foreach (var block in func.BasicBlocks)
            {
                if (block.BlockCondition == ArmConditionCode.Invalid)
                    continue;
                foreach (var inst in block.Instructions)
                {
                    if (inst is Branch b && b.Condition == block.BlockCondition)
                        b.Condition = ArmConditionCode.ARM_CC_AL;
                }
            }

            int varIdx  = 0;
            var regVars = new List<Variable>();
            foreach (var block in func.BasicBlocks)
            {
                foreach (var inst in block.Instructions)
                {
                    while (true)
                    {
                        var def = inst.VariableDefs.FirstOrDefault(v => func.MachineRegisterVariables.Contains(v));
                        if (def == null)
                            break;
                        if (!func.MachineRegisterVariables.Contains(def))
                            continue;
                        //create a new one
                        var regVar = new Variable(VariableLocation.Register, $"rv{varIdx++}", def.Address, 4);
                        regVars.Add(regVar);
                        inst.ReplaceDef(def, regVar);
                    }
                }
            }

            //try merging 64 bit math
            foreach (var block in func.BasicBlocks)
            {
                for (int i = 0; i < block.Instructions.Count; i++)
                {
                    var inst = block.Instructions[i];
                    if (!(inst is ArmMachineInstruction armInst))
                        continue;
                    if (armInst.Instruction.Id == ArmInstructionId.ARM_INS_ADC)
                    {
                        var flagsUseLocs = armInst.VariableUseLocs[(Variable) armInst.FlagsUseOperand];
                        if (flagsUseLocs.Count != 1 ||
                            !(flagsUseLocs.First() is ArmMachineInstruction armInst2) ||
                            armInst2.Instruction.Id != ArmInstructionId.ARM_INS_ADD)
                            continue;
                        //CLoHi = ALoHi + BLoHi
                        var varALo = (Variable) armInst2.Operands[1].op;
                        varALo.LongPart = VariableLongPart.Lo;
                        var varAHi = (Variable) armInst.Operands[1].op;
                        varAHi.LongPart = VariableLongPart.Hi;
                        var varBLo = armInst2.Operands[2].op;
                        if (varBLo is Variable)
                            ((Variable) varBLo).LongPart = VariableLongPart.Lo;
                        var varBHi = armInst.Operands[2].op;
                        if (varBHi is Variable)
                            ((Variable) varBHi).LongPart = VariableLongPart.Hi;
                        var varCLo = (Variable) armInst2.Operands[0].op;
                        varCLo.LongPart = VariableLongPart.Lo;
                        var varCHi = (Variable) armInst.Operands[0].op;
                        varCHi.LongPart = VariableLongPart.Hi;

                        var longAdd = new LongAdd(varCLo, varCHi, varALo, varAHi, varBLo, varBHi);
                        longAdd.VariableUseLocs[varALo] = armInst2.VariableUseLocs[varALo];
                        longAdd.VariableUseLocs[varAHi] = armInst.VariableUseLocs[varAHi];
                        if (varBLo is Variable)
                            longAdd.VariableUseLocs[(Variable) varBLo] = armInst2.VariableUseLocs[(Variable) varBLo];
                        if (varBHi is Variable)
                            longAdd.VariableUseLocs[(Variable) varBHi] = armInst.VariableUseLocs[(Variable) varBHi];
                        longAdd.VariableDefLocs[varCLo] = armInst2.VariableDefLocs[varCLo];
                        longAdd.VariableDefLocs[varCHi] = armInst.VariableDefLocs[varCHi];

                        foreach (var locs in longAdd.VariableUseLocs)
                        {
                            foreach (var loc in locs.Value)
                            {
                                loc.VariableDefLocs[locs.Key].Remove(armInst2);
                                loc.VariableDefLocs[locs.Key].Remove(armInst);
                                loc.VariableDefLocs[locs.Key].Add(longAdd);
                            }
                        }

                        foreach (var locs in longAdd.VariableDefLocs)
                        {
                            foreach (var loc in locs.Value)
                            {
                                loc.VariableUseLocs[locs.Key].Remove(armInst2);
                                loc.VariableUseLocs[locs.Key].Remove(armInst);
                                loc.VariableUseLocs[locs.Key].Add(longAdd);
                            }
                        }

                        if (!block.Instructions.Contains(armInst2))
                            throw new Exception();

                        block.Instructions[block.Instructions.IndexOf(armInst2)] = longAdd;
                        block.Instructions.RemoveAt(i);
                        i--;
                    }
                }
            }

            var irFunc    = new IRFunction();
            var irContext = new IRContext();
            irContext.Function = irFunc;

            foreach (var regVar in regVars)
                irContext.VariableMapping.Add(regVar, new IRRegisterVariable(IRPrimitive.U32, regVar.Name));
            // foreach (var regVar in func.MachineRegisterVariables)
            //     irContext.VariableMapping.Add(regVar, new IRRegisterVariable(IRType.I32, regVar.Name));

            foreach (var stackVar in func.StackVariables)
                irContext.VariableMapping.Add(stackVar, new IRStackVariable(IRPrimitive.U32, stackVar.Name));

            foreach (var basicBlock in func.BasicBlocks)
            {
                var irBB = new IRBasicBlock();
                irBB.OrderIndex = basicBlock.Instructions[0].OrderIndex;
                irContext.BasicBlockMapping.Add(basicBlock, irBB);
                irFunc.BasicBlocks.Add(irBB);
            }

            if (func.Epilogue != null)
                irFunc.Epilogue = irContext.BasicBlockMapping[func.Epilogue];

            foreach (var basicBlock in func.BasicBlocks)
            {
                var irBB = irContext.BasicBlockMapping[basicBlock];
                irBB.Instructions.AddRange(basicBlock.GetIRInstructions(irContext));

                if (irBB.Instructions.Count != 0 && irBB.Instructions.Last() is IRJump jump)
                    irBB.BlockJump = jump;
                irBB.Predecessors.AddRange(basicBlock.Predecessors.Select(p => irContext.BasicBlockMapping[p]));
                irBB.Successors.AddRange(basicBlock.Successors.Select(s => irContext.BasicBlockMapping[s]));
            }

            CalculateDFSOrderIndices(irFunc);
            CalculateImmDominators(irFunc);

            irFunc.BasicBlocks.Sort((a, b) => a.ReversePostOrderIndex.CompareTo(b.ReversePostOrderIndex));

            new ConstantPropagator().Run(irContext);
            new ExpressionPropagator().Run(irContext);
            new DeadCodeEliminator().Run(irContext);

            new MergeS64SubExpressionsPass().Run(irContext);
            new LongShiftFixupPass().Run(irContext);
            new ConstDivisionPass().Run(irContext);

            new FXMulPass().Run(irContext);

            new StructurizeLoopsPass().Run(irContext);
            new CondenseMiniSwitchesPass().Run(irContext);

            new StructurizeIfsPass().Run(irContext);
            new CompoundConditionsPass().Run(irContext);
            new ForLoopsPass().Run(irContext);
            new LifetimeAnalyser().Run(irContext);

            //debug: output dot
            File.WriteAllText(@"basicblocks.txt", func.BasicBlockGraphToDot());
            File.WriteAllText(@"defuse.txt", func.DefUseGraphToDot());
            var u32    = new CType("u32");
            var method = new CMethod(hasReturnValue ? u32 : new CType(), "func");
            if (func.StackVariables.Any(v => v.Address >= 0))
            {
                method.Parameters.Add((u32, "rv0"));
                method.Parameters.Add((u32, "rv1"));
                method.Parameters.Add((u32, "rv2"));
                method.Parameters.Add((u32, "rv3"));

                foreach (var stackVar in func.StackVariables)
                {
                    if (stackVar.Address < 0)
                        continue;
                    method.Parameters.Add((u32, stackVar.Name));
                }
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    bool found = false;
                    foreach (var liveIn in irContext.Function.BasicBlocks[0].LiveIns)
                    {
                        if (ReferenceEquals(irContext.VariableMapping[regVars[i]], liveIn))
                        {
                            method.Parameters.Add((u32, $"rv{i}"));
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                        break;
                }
            }

            method.Body = BasicBlocksToC(irContext);

            return func;
        }

        private static void CalculateDFSOrderIndices(IRFunction func)
        {
            int curFirstId = 0;
            int curLastId  = func.BasicBlocks.Count - 1;
            var dfsStack   = new Stack<(IRBasicBlock node, bool setId)>();
            var dfsVisited = new HashSet<IRBasicBlock>();
            dfsStack.Push((func.BasicBlocks[0], false));
            while (dfsStack.Count > 0)
            {
                var (node, setId) = dfsStack.Pop();
                if (setId)
                {
                    node.ReversePostOrderIndex = curLastId--;
                    continue;
                }

                if (dfsVisited.Contains(node))
                    continue;
                dfsVisited.Add(node);
                node.PreOrderIndex = curFirstId++;
                dfsStack.Push((node, true));
                foreach (var successor in node.Successors)
                    dfsStack.Push((successor, false));
            }
        }

        private static IRBasicBlock GetCommonDominator(IRBasicBlock curImmDom, IRBasicBlock predImmDom)
        {
            if (curImmDom == null)
                return predImmDom;
            if (predImmDom == null)
                return curImmDom;

            while (curImmDom != null && predImmDom != null && curImmDom != predImmDom)
            {
                if (curImmDom.ReversePostOrderIndex < predImmDom.ReversePostOrderIndex)
                    predImmDom = predImmDom.ImmediateDominator;
                else
                    curImmDom = curImmDom.ImmediateDominator;
            }

            return curImmDom;
        }

        private static void CalculateImmDominators(IRFunction func)
        {
            foreach (var block in func.BasicBlocks)
            {
                foreach (var predecessor in block.Predecessors)
                {
                    if (predecessor.ReversePostOrderIndex < block.ReversePostOrderIndex)
                        block.ImmediateDominator = GetCommonDominator(block.ImmediateDominator, predecessor);
                }
            }
        }

        private static CBlock TransformBlocks(IRFunction func, IRBasicBlock head, IRBasicBlock end,
            bool loopBody = false)
        {
            var cBlock    = new CBlock();
            var curBBlock = head;
            while (true)
            {
                IRBasicBlock next = null;
                if (curBBlock == func.Epilogue)
                {
                    if (cBlock.Statements.Count == 0 || !(cBlock.Statements.Last() is CReturn))
                        cBlock.Statements.AddRange(func.Epilogue.Instructions[0].ToCCode());
                    break;
                }

                if (curBBlock == end)
                    break;
                if (curBBlock.ForLoopHead != null && curBBlock.ForLoopHead.LoopType == LoopType.For)
                {
                    cBlock.Statements.AddRange(curBBlock.Instructions.SelectMany(i => i.ToCCode()));
                    curBBlock = curBBlock.ForLoopHead;
                }

                if (curBBlock.LoopHead == curBBlock && (!loopBody || curBBlock != head))
                {
                    var body = TransformBlocks(func, curBBlock, curBBlock.LoopFollow, true);
                    if (curBBlock.LoopType == LoopType.While)
                    {
                        cBlock.Statements.Add(new CWhile(true, body));
                    }
                    else if (curBBlock.LoopType == LoopType.DoWhile)
                    {
                        var latch     = curBBlock.LatchNode;
                        var predicate = latch.BlockJump.Condition.ToCExpression();
                        cBlock.Statements.Add(new CDoWhile(predicate, body));
                    }
                    else if (curBBlock.LoopType == LoopType.For)
                    {
                        var latch     = curBBlock.LatchNode;
                        var predicate = latch.BlockJump.Condition.ToCExpression();
                        var update    = body.Statements.Last();
                        if (!(update is CMethodCall m && m.IsOperator && m.Name == "="))
                            throw new Exception("Expected assignment as for update");
                        body.Statements.Remove(update);
                        cBlock.Statements.Add(new CFor(null, update, predicate, body));
                    }

                    next = curBBlock.LoopFollow;
                }
                else if (curBBlock.IfFollow != null && (!loopBody || head.LatchNode != curBBlock) &&
                         !curBBlock.IsLatchNode)
                {
                    if (curBBlock.LoopHead != null && curBBlock.BlockJump.Destination == curBBlock.LoopHead.LoopFollow)
                    {
                        var predicate = curBBlock.BlockJump.Condition.ToCExpression();
                        cBlock.Statements.Add(new CIf(predicate, new CBlock(new CBreak())));
                        next = curBBlock.Successors.First(s => s != curBBlock.LoopHead.LoopFollow);
                    }
                    else if (curBBlock.LoopHead != null &&
                             curBBlock.LoopHead.LoopType == LoopType.For &&
                             curBBlock.BlockJump.Destination == curBBlock.LoopHead.LatchNode &&
                             curBBlock.Successors.First(s => s != curBBlock.LoopHead.LatchNode).ReversePostOrderIndex <
                             curBBlock.LoopHead.LatchNode.ReversePostOrderIndex)
                    {
                        var predicate = curBBlock.BlockJump.Condition.ToCExpression();
                        cBlock.Statements.Add(new CIf(predicate, new CBlock(new CContinue())));
                        next = curBBlock.Successors.First(s => s != curBBlock.LoopHead.LatchNode);
                    }
                    else
                    {
                        cBlock.Statements.AddRange(curBBlock.Instructions.SelectMany(i => i.ToCCode()));
                        if (curBBlock.Successors.Contains(curBBlock.IfFollow))
                        {
                            var bodyBlockStart = curBBlock.Successors.First(s => s != curBBlock.BlockJump.Destination);

                            CExpression predicate;
                            if (bodyBlockStart.OrderIndex > curBBlock.BlockJump.Destination.OrderIndex)
                            {
                                next           = bodyBlockStart;
                                bodyBlockStart = curBBlock.BlockJump.Destination;
                                predicate      = curBBlock.BlockJump.Condition.ToCExpression();
                            }
                            else
                            {
                                predicate = curBBlock.BlockJump.Condition.InverseCondition().ToCExpression();
                                next      = curBBlock.BlockJump.Destination;
                            }

                            var body = TransformBlocks(func, bodyBlockStart, curBBlock.IfFollow);
                            cBlock.Statements.Add(new CIf(predicate, body));
                        }
                        else
                        {
                            var elseBodyBlockStart = curBBlock.BlockJump.Destination;
                            var ifBodyBlockStart   = curBBlock.Successors.First(s => s != elseBodyBlockStart);

                            CExpression predicate;
                            if (ifBodyBlockStart.OrderIndex > elseBodyBlockStart.OrderIndex)
                            {
                                var tmp = ifBodyBlockStart;
                                ifBodyBlockStart   = elseBodyBlockStart;
                                elseBodyBlockStart = tmp;
                                predicate          = curBBlock.BlockJump.Condition.ToCExpression();
                            }
                            else
                                predicate = curBBlock.BlockJump.Condition.InverseCondition().ToCExpression();

                            var ifBody   = TransformBlocks(func, ifBodyBlockStart, curBBlock.IfFollow);
                            var elseBody = TransformBlocks(func, elseBodyBlockStart, curBBlock.IfFollow);

                            if (ifBody.Statements.Last() is CReturn)
                            {
                                cBlock.Statements.Add(new CIf(predicate, ifBody));
                                cBlock.Statements.AddRange(elseBody.Statements);
                            }
                            else
                                cBlock.Statements.Add(new CIf(predicate, ifBody, elseBody));

                            next = curBBlock.IfFollow;
                        }
                    }
                }
                else
                {
                    cBlock.Statements.AddRange(curBBlock.Instructions.SelectMany(i => i.ToCCode()));
                    if (curBBlock.Successors.Count == 2 &&
                        ((loopBody && head.LatchNode == curBBlock) || curBBlock.IsLatchNode))
                        break;
                    if (curBBlock.Successors.Count == 1)
                    {
                        next = curBBlock.Successors[0];
                    }
                    else if (curBBlock.Successors.Count == 0)
                        break;
                    else
                    {
                        if (curBBlock.SwitchCases.Count == 0)
                            throw new Exception();
                        var cSwitch = new CSwitch(curBBlock.SwitchVariable.ToCExpression());
                        foreach (var (constant, caseBB) in curBBlock.SwitchCases)
                        {
                            var body = TransformBlocks(func, caseBB, curBBlock.SwitchFollow, false);
                            cSwitch.Cases.Add((constant?.ToCExpression() as CLiteral, body));
                        }

                        cBlock.Statements.Add(cSwitch);
                        next = curBBlock.SwitchFollow;
                    }
                }

                if ((curBBlock.IsLatchNode && curBBlock.LoopHead != curBBlock) ||
                    (loopBody && next.LoopHead != head.LoopHead))
                    break;
                curBBlock = next;
            }

            return cBlock;
        }

        private static CBlock BasicBlocksToC(IRContext context)
        {
            return TransformBlocks(context.Function, context.Function.BasicBlocks[0], null);
        }

        /// <summary>
        /// Reintroduces basic blocks for conditionals
        /// </summary>
        private static void RecreateConditionalBasicBlocks(Function func)
        {
            while (true)
            {
                bool invalidated = false;
                foreach (var block in func.BasicBlocks)
                {
                    var blockCondition = block.BlockCondition == ArmConditionCode.Invalid
                        ? ArmConditionCode.ARM_CC_AL
                        : block.BlockCondition;
                    for (int i = 0; i < block.Instructions.Count; i++)
                    {
                        if (!(block.Instructions[i] is ArmMachineInstruction mInst) ||
                            ArmUtil.IsJump(mInst.Instruction))
                            continue;
                        if (mInst.Condition == blockCondition)
                            continue;
                        var oppCc = ArmUtil.GetOppositeCondition(mInst.Condition);
                        //find the first instruction with opposite condition or no condition
                        int j = i + 1;
                        if (j < block.Instructions.Count)
                        {
                            while (j < block.Instructions.Count &&
                                   block.Instructions[j].Condition != oppCc &&
                                   block.Instructions[j].Condition != ArmConditionCode.ARM_CC_AL)
                                j++;

                            if (j < block.Instructions.Count && block.Instructions[j].Condition == oppCc)
                            {
                                int k = j + 1;
                                while (k < block.Instructions.Count &&
                                       block.Instructions[k].Condition != ArmConditionCode.ARM_CC_AL)
                                    k++;

                                var newBlock1 = new BasicBlock(
                                    (uint) ((ArmMachineInstruction) block.Instructions[i]).Instruction.Address,
                                    mInst.Condition);
                                newBlock1.BlockConditionInstruction =
                                    mInst.VariableUseLocs[func.MachineRegisterVariables[16]].First();
                                newBlock1.Instructions.AddRange(block.Instructions.Skip(i).Take(j - i));
                                block.Instructions.RemoveRange(i, j - i);

                                var newBlock2 = new BasicBlock(
                                    (uint) ((ArmMachineInstruction) block.Instructions[i]).Instruction.Address,
                                    oppCc);
                                newBlock2.BlockConditionInstruction =
                                    mInst.VariableUseLocs[func.MachineRegisterVariables[16]].First();
                                newBlock2.Instructions.AddRange(block.Instructions.Skip(i).Take(k - j));
                                block.Instructions.RemoveRange(i, k - j);

                                BasicBlock newBlock3 = null;

                                if (block.Instructions.Count - i > 0)
                                {
                                    newBlock3 = new BasicBlock(
                                        (uint) ((ArmMachineInstruction) block.Instructions[i]).Instruction.Address);
                                    newBlock3.Instructions.AddRange(block.Instructions.Skip(i));
                                    block.Instructions.RemoveRange(i, block.Instructions.Count - i);
                                    newBlock3.Successors.AddRange(block.Successors);

                                    foreach (var successor in block.Successors)
                                    {
                                        successor.Predecessors.Remove(block);
                                        successor.Predecessors.Add(newBlock3);
                                    }
                                }
                                else
                                {
                                    if (!(newBlock1.Instructions.Last() is ArmMachineInstruction lastMInst4) ||
                                        lastMInst4.Instruction.Id != ArmInstructionId.ARM_INS_BX)
                                    {
                                        if (newBlock1.Instructions.Last() is Branch b)
                                        {
                                            newBlock1.Successors.Add(b.Destination);
                                            b.Destination.Predecessors.Add(newBlock1);

                                            block.Successors.Remove(b.Destination);
                                            b.Destination.Predecessors.Remove(block);
                                        }
                                        else
                                        {
                                            newBlock1.Successors.AddRange(block.Successors);
                                            foreach (var successor in block.Successors)
                                                successor.Predecessors.Add(newBlock1);
                                        }
                                    }

                                    if (!(newBlock2.Instructions.Last() is ArmMachineInstruction lastMInst5) ||
                                        lastMInst5.Instruction.Id != ArmInstructionId.ARM_INS_BX)
                                    {
                                        if (newBlock2.Instructions.Last() is Branch b)
                                        {
                                            newBlock2.Successors.Add(b.Destination);
                                            b.Destination.Predecessors.Add(newBlock2);

                                            block.Successors.Remove(b.Destination);
                                            b.Destination.Predecessors.Remove(block);
                                        }
                                        else
                                        {
                                            newBlock2.Successors.AddRange(block.Successors);
                                            foreach (var successor in block.Successors)
                                                successor.Predecessors.Add(newBlock2);
                                        }
                                    }
                                }

                                block.Successors.Clear();

                                block.Successors.Add(newBlock1);
                                newBlock1.Predecessors.Add(block);
                                block.Successors.Add(newBlock2);
                                newBlock2.Predecessors.Add(block);

                                block.BlockBranch = new Branch(oppCc, newBlock2, func.MachineRegisterVariables[16]);
                                block.Instructions.Add(block.BlockBranch);

                                if (!(newBlock1.Instructions.Last() is ArmMachineInstruction lastMInst) ||
                                    lastMInst.Instruction.Id != ArmInstructionId.ARM_INS_BX)
                                {
                                    if (newBlock3 != null)
                                    {
                                        newBlock1.Successors.Add(newBlock3);
                                        newBlock3.Predecessors.Add(newBlock1);
                                        newBlock1.BlockBranch = new Branch(newBlock1.BlockCondition, newBlock3,
                                            func.MachineRegisterVariables[16]);
                                        newBlock1.Instructions.Add(newBlock1.BlockBranch);
                                    }
                                }

                                if (!(newBlock2.Instructions.Last() is ArmMachineInstruction lastMInst2) ||
                                    lastMInst2.Instruction.Id != ArmInstructionId.ARM_INS_BX)
                                {
                                    if (newBlock3 != null)
                                    {
                                        newBlock2.Successors.Add(newBlock3);
                                        newBlock3.Predecessors.Add(newBlock2);
                                    }
                                }

                                func.BasicBlocks.Add(newBlock1);
                                func.BasicBlocks.Add(newBlock2);

                                if (newBlock3 != null)
                                    func.BasicBlocks.Add(newBlock3);

                                invalidated = true;
                                break;
                            }
                        }

                        var newBlock = new BasicBlock((uint) mInst.Instruction.Address, mInst.Condition);
                        newBlock.BlockConditionInstruction =
                            mInst.VariableUseLocs[func.MachineRegisterVariables[16]].First();
                        newBlock.Instructions.AddRange(block.Instructions.Skip(i).Take(j - i));
                        block.Instructions.RemoveRange(i, j - i);

                        BasicBlock newBlock4 = null;
                        if (block.Instructions.Count - i > 0)
                        {
                            newBlock4 = new BasicBlock(
                                (uint) ((ArmMachineInstruction) block.Instructions[i]).Instruction.Address);
                            newBlock4.Instructions.AddRange(block.Instructions.Skip(i));
                            block.Instructions.RemoveRange(i, block.Instructions.Count - i);
                            newBlock4.Successors.AddRange(block.Successors);
                            foreach (var successor in block.Successors)
                            {
                                successor.Predecessors.Remove(block);
                                successor.Predecessors.Add(newBlock4);
                            }

                            block.Successors.Clear();
                        }
                        else if (!(newBlock.Instructions.Last() is ArmMachineInstruction lastMInst4) ||
                                 lastMInst4.Instruction.Id != ArmInstructionId.ARM_INS_BX)
                        {
                            if (newBlock.Instructions.Last() is Branch b)
                            {
                                newBlock.Successors.Add(b.Destination);
                                b.Destination.Predecessors.Add(newBlock);

                                block.Successors.Remove(b.Destination);
                                b.Destination.Predecessors.Remove(block);
                            }
                            else
                            {
                                newBlock.Successors.AddRange(block.Successors);
                                foreach (var successor in block.Successors)
                                    successor.Predecessors.Add(newBlock);
                            }
                        }


                        block.Successors.Add(newBlock);
                        newBlock.Predecessors.Add(block);

                        if (newBlock4 != null)
                        {
                            block.Successors.Add(newBlock4);
                            newBlock4.Predecessors.Add(block);

                            block.BlockBranch = new Branch(oppCc, newBlock4, func.MachineRegisterVariables[16]);
                            block.Instructions.Add(block.BlockBranch);

                            if (!(newBlock.Instructions.Last() is ArmMachineInstruction lastMInst3) ||
                                lastMInst3.Instruction.Id != ArmInstructionId.ARM_INS_BX)
                            {
                                newBlock.Successors.Add(newBlock4);
                                newBlock4.Predecessors.Add(newBlock);
                            }

                            if (newBlock4.Instructions.Last() is Branch b)
                                newBlock4.BlockBranch = b;
                        }
                        else
                        {
                            block.BlockBranch = new Branch(oppCc, block.Successors.First(s => s != newBlock),
                                func.MachineRegisterVariables[16]);
                            block.Instructions.Add(block.BlockBranch);
                        }

                        func.BasicBlocks.Add(newBlock);

                        if (newBlock4 != null)
                            func.BasicBlocks.Add(newBlock4);

                        invalidated = true;
                        break;
                    }

                    if (invalidated)
                        break;
                }

                if (!invalidated)
                    break;
            }

            //fixup def-use
            foreach (var block in func.BasicBlocks)
            {
                foreach (var inst in block.Instructions)
                {
                    inst.VariableDefLocs.Clear();
                    inst.VariableUseLocs.Clear();
                }
            }

            ResolveDefUseRelations(func);
        }

        /// <summary>
        /// Unscheduling of mixed up conditionals
        /// </summary>
        private static void DescheduleConditionals(Function func)
        {
            //todo: check for side effects (read/write order on volatile memory and such)
            foreach (var block in func.BasicBlocks)
            {
                for (int i = 0; i < block.Instructions.Count; i++)
                {
                    var instruction = block.Instructions[i];
                    if (instruction.Condition == ArmConditionCode.ARM_CC_AL)
                        continue;
                    //find all instructions that depend on the same flag producing instruction and use the same condition code
                    var cInsts = block.Instructions
                        .Where(inst =>
                            inst is ArmMachineInstruction mInst2 &&
                            mInst2.Condition == instruction.Condition &&
                            inst.VariableUses.Contains(func.MachineRegisterVariables[16]) &&
                            inst.VariableUseLocs[func.MachineRegisterVariables[16]].First() ==
                            instruction.VariableUseLocs[func.MachineRegisterVariables[16]].First())
                        .ToArray();
                    //Assume at most one split is made
                    int                   firstPartLength  = cInsts.Length;
                    int                   secondPartLength = 0;
                    ArmMachineInstruction secondPartStart  = null;
                    for (int j = 0; j < cInsts.Length - 1; j++)
                    {
                        var inst     = (ArmMachineInstruction) cInsts[j];
                        var nextInst = (ArmMachineInstruction) cInsts[j + 1];
                        if (inst.Instruction.Address + inst.Instruction.Bytes.Length !=
                            nextInst.Instruction.Address)
                        {
                            firstPartLength  = j + 1;
                            secondPartLength = cInsts.Length - firstPartLength;
                            secondPartStart  = nextInst;
                        }
                    }

                    if (secondPartLength == 0)
                        continue;

                    if (cInsts
                        .Take(firstPartLength)
                        .All(inst =>
                            inst.VariableDefLocs
                                .SelectMany(dl => dl.Value)
                                .Where(inst2 => !cInsts.Take(firstPartLength).Contains(inst2))
                                .All(inst2 =>
                                    !(inst2 is ArmMachineInstruction m) ||
                                    m.Instruction.Address >= secondPartStart.Instruction.Address)))
                    {
                        block.Instructions.RemoveRange(i, firstPartLength);
                        block.Instructions.InsertRange(block.Instructions.IndexOf(secondPartStart),
                            cInsts.Take(firstPartLength));
                        i = block.Instructions.IndexOf(cInsts.Last());
                    }
                    else
                        throw new Exception("Couldn't unschedule!");
                }
            }
        }

        /// <summary>
        /// Merge the conditional blocks back into the parent blocks
        /// </summary>
        private static void FlattenConditionals(Function func)
        {
            while (true)
            {
                var block = func.BasicBlocks.FirstOrDefault(b => b.BlockCondition != ArmConditionCode.Invalid);
                if (block == null)
                    break;
                if (block.Predecessors.Count != 1)
                    throw new Exception("Expected exactly one predecessor!");
                block.Predecessors[0].MergeAppend(block);
                func.BasicBlocks.Remove(block);
            }

            while (true)
            {
                var block = func.BasicBlocks.FirstOrDefault(b =>
                    b.Successors.Count == 1 && b.Successors[0] != func.Epilogue &&
                    b.Successors[0].Predecessors.Count == 1 &&
                    !((b.Instructions.Last() is ArmMachineInstruction m && ArmUtil.IsJump(m.Instruction)) ||
                      b.Instructions.Last() is Branch));
                if (block == null)
                    break;
                var succ = block.Successors[0];
                block.MergeAppend(succ);
                func.BasicBlocks.Remove(succ);
            }
        }
    }
}