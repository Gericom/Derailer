using System.Linq;
using Gee.External.Capstone.Arm;
using LibDerailer.CCodeGen.Statements;
using LibDerailer.CCodeGen.Statements.Expressions;

namespace LibDerailer.CodeGraph.Nodes
{
    public class LoadConstant : Instruction
    {
        public uint Constant { get; }

        public LoadConstant(ArmConditionCode condition, Variable dst, uint constant)
            : base(condition)
        {
            Constant = constant;
            VariableDefs.Add(dst);
            Operands.Add((true, dst));
        }

        public override string ToString() => $"{Operands[0].op} = 0x{Constant:X08}";

        public override CStatement[] GetCode() => new CStatement[]
        {
            CExpression.Assign(new CVariable(((Variable) Operands[0].op).Name), Constant)
        };
    }
}