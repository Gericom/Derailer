using System.Collections.Generic;
using System.Linq;
using Gee.External.Capstone.Arm;
using LibDerailer.CCodeGen.Statements;

namespace LibDerailer.CodeGraph.Nodes
{
    public abstract class Instruction
    {
        public ArmConditionCode  Condition    { get; }
        public HashSet<Variable> VariableUses { get; } = new HashSet<Variable>();
        public HashSet<Variable> VariableDefs { get; } = new HashSet<Variable>();

        public Dictionary<Variable, HashSet<Instruction>> VariableUseLocs { get; } =
            new Dictionary<Variable, HashSet<Instruction>>();

        public Dictionary<Variable, HashSet<Instruction>> VariableDefLocs { get; } =
            new Dictionary<Variable, HashSet<Instruction>>();

        public Operand FlagsUseOperand { get; protected set; }
        public Operand FlagsDefOperand { get; protected set; }

        public List<(bool isDef, Operand op)> Operands { get; } = new List<(bool isDef, Operand op)>();

        public Instruction(ArmConditionCode condition)
        {
            Condition = condition;
        }

        public void ReplaceDef(Variable oldVar, Variable newVar)
        {
            if (!VariableDefs.Contains(oldVar))
                return;
            VariableDefs.Remove(oldVar);
            VariableDefs.Add(newVar);
            for (int i = 0; i < Operands.Count; i++)
                if (Operands[i].isDef && Operands[i].op == oldVar)
                    Operands[i] = (true, newVar);
            if (FlagsDefOperand == oldVar)
                FlagsDefOperand = newVar;
            if (VariableDefLocs.ContainsKey(oldVar))
            {
                VariableDefLocs.Add(newVar, VariableDefLocs[oldVar]);
                VariableDefLocs.Remove(oldVar);
                foreach (var useLoc in VariableDefLocs[newVar])
                    useLoc.ReplaceUse(oldVar, newVar);
            }
        }

        public void ReplaceUse(Variable oldVar, Variable newVar)
        {
            if (!VariableUses.Contains(oldVar))
                return;
            VariableUses.Remove(oldVar);
            VariableUses.Add(newVar);
            for (int i = 0; i < Operands.Count; i++)
                if (!Operands[i].isDef && Operands[i].op == oldVar)
                    Operands[i] = (false, newVar);
            if (FlagsUseOperand == oldVar)
                FlagsUseOperand = newVar;
            if (VariableUseLocs.ContainsKey(oldVar))
            {
                VariableUseLocs.Add(newVar, VariableUseLocs[oldVar]);
                VariableUseLocs.Remove(oldVar);
                foreach (var defLoc in VariableUseLocs[newVar])
                    defLoc.ReplaceDef(oldVar, newVar);
            }
        }

        public virtual CStatement[] GetCode()
            => new CStatement[0];
    }
}