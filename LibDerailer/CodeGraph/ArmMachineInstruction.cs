using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gee.External.Capstone.Arm;

namespace LibDerailer.CodeGraph
{
    public class ArmMachineInstruction : Instruction
    {
        public ArmInstruction Instruction { get; }

        public ArmMachineInstruction(ArmInstruction instruction)
        {
            Instruction = instruction;
        }

        public override string ToString() => Instruction.Mnemonic + " " + Instruction.Operand;
    }
}