using System.Collections.Generic;
using System.Linq;
using Gee.External.Capstone.Arm;
using LibDerailer.CCodeGen.Statements;
using LibDerailer.CCodeGen.Statements.Expressions;
using LibDerailer.IR;
using LibDerailer.IR.Instructions;

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

        public override IEnumerable<IRInstruction> GetIRInstructions(IRContext context, IRBasicBlock parentBlock)
        {
            yield return new IRAssignment(parentBlock, context.VariableMapping[(Variable)Operands[0].op], Constant);
        }
    }
}