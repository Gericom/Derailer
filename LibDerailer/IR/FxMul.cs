using System.Linq;
using Gee.External.Capstone.Arm;
using LibDerailer.CodeGraph;
using LibDerailer.CodeGraph.Nodes;

namespace LibDerailer.IR
{
    public class FxMul : Instruction
    {
        public FxMul(Variable dst, Variable srcA, Variable srcB, bool updatesFlags, Variable dstFlags)
            : base(ArmConditionCode.ARM_CC_AL)
        {
            Dst          = dst;
            SrcA         = srcA;
            SrcB         = srcB;
            UpdatesFlags = updatesFlags;
            DstFlags     = dstFlags;
            VariableUses.Add(srcA);
            VariableUses.Add(srcB);
            VariableDefs.Add(dst);
            if (UpdatesFlags)
                VariableDefs.Add(dstFlags);
            Operands.Add((true, dst));
            Operands.Add((false, srcA));
            Operands.Add((false, srcB));
            FlagsDefOperand = DstFlags;
        }

        public bool UpdatesFlags { get; }

        public Variable SrcA     { get; }
        public Variable SrcB     { get; }
        public Variable Dst      { get; }
        public Variable DstFlags { get; }

        public override string ToString() => $"{Dst} = FX_Mul({SrcA}, {SrcB})";

        public static FxMul Match(BasicBlock block, Instruction inst)
        {
            if (!(inst is ArmMachineInstruction mInst))
                return null;
            if (mInst.Instruction.Id != ArmInstructionId.ARM_INS_SMULL)
                return null;
            var rdLo = (Variable) mInst.Operands[0].op;
            var rdHi = (Variable) mInst.Operands[1].op;
            if (mInst.VariableDefLocs[rdLo].Count != 1 || mInst.VariableDefLocs[rdHi].Count != 1)
                return null;
            if (!(mInst.VariableDefLocs[rdLo].First() is ArmMachineInstruction adds) ||
                !(adds.Instruction.Id == ArmInstructionId.ARM_INS_ADD && adds.Instruction.Details.UpdateFlags))
                return null;

            var addsRm = adds.Operands[2].op as Variable;
            if (addsRm == null ||
                adds.VariableUseLocs[addsRm].Count != 1 ||
                !(adds.VariableUseLocs[addsRm].First() is ArmMachineInstruction mov) ||
                mov.Instruction.Id != ArmInstructionId.ARM_INS_MOV ||
                !(mov.Operands[1].op is Constant movConst) ||
                movConst.Value != 2048)
                return null;

            if (!(mInst.VariableDefLocs[rdHi].First() is ArmMachineInstruction adc) ||
                adc.Instruction.Id != ArmInstructionId.ARM_INS_ADC)
                return null;
            if (adc.FlagsUseOperand != adds.FlagsDefOperand)
                return null;
            if (!(adc.Operands[2].op is Constant adcImm) || adcImm.Value != 0)
                return null;
            var addsRd = (Variable) adds.Operands[0].op;
            if (adds.VariableDefLocs[addsRd].Count != 1 ||
                !(adds.VariableDefLocs[addsRd].First() is ArmMachineInstruction lsr) ||
                lsr.Instruction.Id != ArmInstructionId.ARM_INS_LSR ||
                lsr.Instruction.Details.Operands[1].ShiftValue != 12)
                return null;
            var adcRd = (Variable) adc.Operands[0].op;
            if (adc.VariableDefLocs[adcRd].Count != 1 ||
                !(adc.VariableDefLocs[adcRd].First() is ArmMachineInstruction orr) ||
                orr.Instruction.Id != ArmInstructionId.ARM_INS_ORR ||
                orr.Instruction.Details.Operands[2].ShiftOperation != ArmShiftOperation.ARM_SFT_LSL ||
                orr.Instruction.Details.Operands[2].ShiftValue != 20 ||
                orr.Operands[1].op != lsr.Operands[0].op ||
                orr.Operands[2].op != adc.Operands[0].op)
                return null;

            var fxMul = new FxMul(
                (Variable) orr.Operands[0].op,
                (Variable) mInst.Operands[2].op,
                (Variable) mInst.Operands[3].op,
                orr.Instruction.Details.UpdateFlags,
                orr.Instruction.Details.UpdateFlags ? (Variable) orr.FlagsDefOperand : null
            );
            fxMul.VariableUseLocs[fxMul.SrcA] = mInst.VariableUseLocs[fxMul.SrcA];
            fxMul.VariableUseLocs[fxMul.SrcB] = mInst.VariableUseLocs[fxMul.SrcB];
            if (fxMul.UpdatesFlags && orr.VariableDefLocs.ContainsKey(fxMul.DstFlags))
                fxMul.VariableDefLocs[fxMul.DstFlags] = orr.VariableDefLocs[fxMul.DstFlags];


            if (orr.VariableDefLocs.ContainsKey(fxMul.Dst))
            {
                fxMul.VariableDefLocs[fxMul.Dst] = orr.VariableDefLocs[fxMul.Dst];
                foreach (var useLoc in fxMul.VariableDefLocs[fxMul.Dst])
                {
                    useLoc.VariableUseLocs[fxMul.Dst].Remove(orr);
                    useLoc.VariableUseLocs[fxMul.Dst].Add(fxMul);
                }
            }

            int idx = block.Instructions.IndexOf(mInst);

            block.Instructions.Remove(mInst);
            block.Instructions.Remove(adc);
            block.Instructions.Remove(adds);
            block.Instructions.Remove(lsr);
            block.Instructions.Remove(orr);
            block.Instructions.Insert(idx, fxMul);

            return fxMul;
        }
    }
}