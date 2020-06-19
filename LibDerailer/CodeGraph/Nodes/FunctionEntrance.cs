using Gee.External.Capstone.Arm;

namespace LibDerailer.CodeGraph.Nodes
{
    public class FunctionEntrance : Instruction
    {
        public FunctionEntrance(Function func, Variable[] regVars)
            : base(func.Address, ArmConditionCode.ARM_CC_AL)
        {
            VariableDefs.Add(regVars[0]);
            VariableDefs.Add(regVars[1]);
            VariableDefs.Add(regVars[2]);
            VariableDefs.Add(regVars[3]);
            VariableDefs.Add(regVars[13]);
            VariableDefs.Add(regVars[14]);
            foreach (var stackVar in func.StackVariables)
                if (stackVar.Address >= 0)
                    VariableDefs.Add(stackVar);
        }

        public override string ToString() => "Function Entrance";
    }
}