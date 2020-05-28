using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.CCodeGen;
using LibDerailer.CCodeGen.Statements;
using LibDerailer.CCodeGen.Statements.Expressions;
using LibDerailer.IR.Expressions;

namespace LibDerailer.IR.Instructions
{
    public class IRStore : IRInstruction
    {
        public IRType       Type    { get; }
        public IRExpression Address { get; set; }
        public IRExpression Operand { get; set; }

        public bool IsSigned { get; set; }

        public IRStore(IRBasicBlock parentBlock, bool isStore, IRType type, bool isSigned, IRExpression address,
            IRExpression operand)
            : base(parentBlock)
        {
            if (type == IRType.Void || type == IRType.I1)
                throw new IRTypeException();
            Type     = type;
            IsSigned = isSigned;
            Address  = address;
            Operand  = operand;
            Uses.UnionWith(Address.GetAllVariables());
            Uses.UnionWith(Operand.GetAllVariables());
        }

        public override IEnumerable<CStatement> ToCCode()
        {
            yield return CExpression.Assign(
                CExpression.Deref(new CCast(new CType(Type.ToCType(IsSigned), true), Address.ToCExpression())),
                Operand.ToCExpression());
        }

        public override void SubstituteUse(IRVariable variable, IRExpression expression)
        {
            if (ReferenceEquals(Address, variable))
                Address = expression.CloneComplete();
            else
                Address.Substitute(variable, expression);

            Uses.Clear();
            Uses.UnionWith(Address.GetAllVariables());

            if (ReferenceEquals(Operand, variable))
                Operand = expression.CloneComplete();
            else
                Operand.Substitute(variable, expression);

            Uses.UnionWith(Operand.GetAllVariables());
        }

        public override void SubstituteDef(IRVariable variable, IRExpression expression)
        {
            
        }
    }
}