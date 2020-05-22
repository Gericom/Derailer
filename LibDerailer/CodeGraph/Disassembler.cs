using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gee.External.Capstone;
using Gee.External.Capstone.Arm;
using LibDerailer.CodeGraph.Nodes;
using LibDerailer.CodeGraph.Nodes.IR;
using LibDerailer.IO;

namespace LibDerailer.CodeGraph
{
    public class Disassembler
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
            var disassembler = CapstoneDisassembler.CreateArmDisassembler(mode);
            disassembler.DisassembleSyntax        = (DisassembleSyntax) 3;
            disassembler.EnableInstructionDetails = true;
            disassembler.EnableSkipDataMode       = true;
            var disResult = disassembler.Disassemble(data, dataAddress);

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

                    if (ArmUtil.IsJump(inst))
                    {
                        //Branch
                        curBlock.Instructions.Add(new ArmMachineInstruction(inst, func.MachineRegisterVariables));


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

                            // if (inst.Id == ArmInstructionId.ARM_INS_LDR)
                            //     graphInst.VariableUses.Add(stackVar);
                            // else
                            //     graphInst.VariableDefs.Add(stackVar);
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
            func.BasicBlocks[0].Instructions.Insert(0, new FunctionEntrance(func, func.MachineRegisterVariables));
            ResolveDefUseRelations(func);
            FlattenConditionals(func);
            UnscheduleConditionals(func);
            RecreateConditionalBasicBlocks(func);

            //create fresh reg variables
            int varIdx = 0;
            var regVars = new List<Variable>();
            foreach (var block in func.BasicBlocks)
            {
                foreach (var inst in block.Instructions)
                {
                    while(true)
                    {
                        var def = inst.VariableDefs.FirstOrDefault(v => func.MachineRegisterVariables.Contains(v));
                        if (def == null)
                            break;
                        if (!func.MachineRegisterVariables.Contains(def))
                            continue;
                        //create a new one
                        var regVar = new Variable(VariableLocation.Register, $"regVar_{varIdx++}", def.Address, 4);
                        regVars.Add(regVar);
                        inst.ReplaceDef(def, regVar);   
                    }
                }
            }

            foreach (var block in func.BasicBlocks)
            {
                for(int i = 0; i < block.Instructions.Count; i++)
                {
                    FxMul.Match(block, block.Instructions[i]);
                }
            }

            func.BasicBlocks.Sort();

            //debug: output dot
            File.WriteAllText(@"basicblocks.txt", func.BasicBlockGraphToDot());
            File.WriteAllText(@"defuse.txt", func.DefUseGraphToDot());
           

            return func;
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
                        if (mInst.Instruction.Details.ConditionCode == blockCondition)
                            continue;
                        var oppCc = ArmUtil.GetOppositeCondition(mInst.Instruction.Details.ConditionCode);
                        //find the first instruction with opposite condition or no condition
                        int j = i + 1;
                        if (j < block.Instructions.Count)
                        {
                            while (j < block.Instructions.Count &&
                                   ((ArmMachineInstruction) block.Instructions[j]).Instruction.Details.ConditionCode !=
                                   oppCc &&
                                   ((ArmMachineInstruction) block.Instructions[j]).Instruction.Details.ConditionCode !=
                                   ArmConditionCode.ARM_CC_AL)
                                j++;

                            if (j < block.Instructions.Count &&
                                ((ArmMachineInstruction) block.Instructions[j]).Instruction.Details.ConditionCode ==
                                oppCc)
                            {
                                int k = j + 1;
                                while (((ArmMachineInstruction) block.Instructions[k]).Instruction.Details
                                       .ConditionCode !=
                                       ArmConditionCode.ARM_CC_AL)
                                    k++;

                                var newBlock1 = new BasicBlock(
                                    (uint) ((ArmMachineInstruction) block.Instructions[i]).Instruction.Address,
                                    mInst.Instruction.Details.ConditionCode);
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

                                var newBlock3 = new BasicBlock(
                                    (uint) ((ArmMachineInstruction) block.Instructions[i]).Instruction.Address);
                                newBlock3.Instructions.AddRange(block.Instructions.Skip(i));
                                block.Instructions.RemoveRange(i, block.Instructions.Count - i);
                                newBlock3.Successors.AddRange(block.Successors);

                                foreach (var successor in block.Successors)
                                {
                                    successor.Predecessors.Remove(block);
                                    successor.Predecessors.Add(newBlock3);
                                }

                                block.Successors.Clear();

                                block.Successors.Add(newBlock1);
                                newBlock1.Predecessors.Add(block);
                                block.Successors.Add(newBlock2);
                                newBlock2.Predecessors.Add(block);

                                if (!(newBlock1.Instructions.Last() is ArmMachineInstruction lastMInst) ||
                                    lastMInst.Instruction.Id != ArmInstructionId.ARM_INS_BX)
                                {
                                    newBlock1.Successors.Add(newBlock3);
                                    newBlock3.Predecessors.Add(newBlock1);
                                }

                                if (!(newBlock2.Instructions.Last() is ArmMachineInstruction lastMInst2) ||
                                    lastMInst2.Instruction.Id != ArmInstructionId.ARM_INS_BX)
                                {
                                    newBlock2.Successors.Add(newBlock3);
                                    newBlock3.Predecessors.Add(newBlock2);
                                }

                                func.BasicBlocks.Add(newBlock1);
                                func.BasicBlocks.Add(newBlock2);
                                func.BasicBlocks.Add(newBlock3);

                                invalidated = true;
                                break;
                            }
                        }

                        var newBlock = new BasicBlock((uint) mInst.Instruction.Address,
                            mInst.Instruction.Details.ConditionCode);
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
                            newBlock.Successors.AddRange(block.Successors);
                            foreach (var successor in block.Successors)
                                successor.Predecessors.Add(newBlock);
                        }


                        block.Successors.Add(newBlock);
                        newBlock.Predecessors.Add(block);

                        if (newBlock4 != null)
                        {
                            block.Successors.Add(newBlock4);
                            newBlock4.Predecessors.Add(block);

                            if (!(newBlock.Instructions.Last() is ArmMachineInstruction lastMInst3) ||
                                lastMInst3.Instruction.Id != ArmInstructionId.ARM_INS_BX)
                            {
                                newBlock.Successors.Add(newBlock4);
                                newBlock4.Predecessors.Add(newBlock);
                            }
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
        private static void UnscheduleConditionals(Function func)
        {
            //todo: check for sideeffects (read/write order on volatile memory and such)
            foreach (var block in func.BasicBlocks)
            {
                for (int i = 0; i < block.Instructions.Count; i++)
                {
                    if (!(block.Instructions[i] is ArmMachineInstruction mInst))
                        continue;
                    if (mInst.Instruction.Details.ConditionCode == ArmConditionCode.ARM_CC_AL)
                        continue;
                    //find all instructions that depend on the same flag producing instruction and use the same condition code
                    var cInsts = block.Instructions
                        .Where(inst =>
                            inst is ArmMachineInstruction mInst2 &&
                            mInst2.Instruction.Details.ConditionCode == mInst.Instruction.Details.ConditionCode &&
                            inst.VariableUses.Contains(func.MachineRegisterVariables[16]) &&
                            inst.VariableUseLocs[func.MachineRegisterVariables[16]].First() ==
                            mInst.VariableUseLocs[func.MachineRegisterVariables[16]].First())
                        .ToArray();
                    var revCond = ArmUtil.GetOppositeCondition(mInst.Instruction.Details.ConditionCode);
                    var cInsts2 = block.Instructions
                        .Where(inst =>
                            inst is ArmMachineInstruction mInst2 &&
                            mInst2.Instruction.Details.ConditionCode == revCond &&
                            inst.VariableUses.Contains(func.MachineRegisterVariables[16]) &&
                            inst.VariableUseLocs[func.MachineRegisterVariables[16]].First() ==
                            mInst.VariableUseLocs[func.MachineRegisterVariables[16]].First())
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

                    if (secondPartLength != 0)
                    {
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
                    b.Successors.Count == 1 && b.Successors[0].Predecessors.Count == 1 &&
                    !(b.Instructions.Last() is ArmMachineInstruction m && ArmUtil.IsJump(m.Instruction)));
                if (block == null)
                    break;
                var succ = block.Successors[0];
                block.MergeAppend(succ);
                func.BasicBlocks.Remove(succ);
            }
        }
    }
}