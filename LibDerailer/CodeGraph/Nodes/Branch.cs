using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gee.External.Capstone.Arm;
using LibDerailer.CCodeGen.Statements;

namespace LibDerailer.CodeGraph.Nodes
{
    public class Branch : Instruction
    {
        public BasicBlock Destination { get; }

        public Branch(ArmConditionCode condition, BasicBlock destination, Variable cpsr = null) 
            : base(condition)
        {
            Destination = destination;
            if (condition != ArmConditionCode.ARM_CC_AL && cpsr != null)
            {
                VariableUses.Add(cpsr);
                FlagsUseOperand = cpsr;
            }
        }
    }
}
