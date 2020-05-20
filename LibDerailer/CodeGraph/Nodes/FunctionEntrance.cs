namespace LibDerailer.CodeGraph.Nodes
{
    public class FunctionEntrance : Instruction
    {
        public FunctionEntrance(Function func, Variable[] regVars)
        {
            VariableDefs.Add(regVars[0]);
            VariableDefs.Add(regVars[1]);
            VariableDefs.Add(regVars[2]);
            VariableDefs.Add(regVars[3]);
            foreach (var stackVar in func.StackVariables)
                if (stackVar.Address >= 0)
                    VariableDefs.Add(stackVar);
        }

        public override string ToString() => "Function Entrance";
    }
}
