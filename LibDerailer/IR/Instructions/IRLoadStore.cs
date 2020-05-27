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
    public class IRLoadStore : IRInstruction
    {
        public IRType       Type    { get; }
        public IRExpression Address { get; set; }
        public IRExpression Operand { get; set; }

        public bool IsSigned { get; set; }

        public bool IsStore { get; set; }

        public IRLoadStore(IRBasicBlock parentBlock, bool isStore, IRType type, bool isSigned, IRExpression address,
            IRExpression operand)
            : base(parentBlock)
        {
            if (type == IRType.Void || type == IRType.I1)
                throw new IRTypeException();
            IsStore  = isStore;
            Type     = type;
            IsSigned = isSigned;
            Address  = address;
            Operand  = operand;
        }

        public override IEnumerable<CStatement> ToCCode()
        {
            if (IsStore)
                yield return CExpression.Assign(
                    CExpression.Deref(new CCast(new CType(Type.ToCType(IsSigned), true), Address.ToCExpression())),
                    Operand.ToCExpression());
            else
                yield return CExpression.Assign(
                    Operand.ToCExpression(),
                    CExpression.Deref(new CCast(new CType(Type.ToCType(IsSigned), true), Address.ToCExpression())));
        }

        public override void Substitute(IRVariable variable, IRExpression expression)
        {
            if (ReferenceEquals(Address, variable))
                Address = expression;
            else
                Address.Substitute(variable, expression);

            if (ReferenceEquals(Operand, variable))
                Operand = expression;
            else
                Operand.Substitute(variable, expression);

            if (variable.Uses.Contains(this))
                foreach (var v in expression.GetAllVariables())
                    v.Uses.Add(this);

            if (variable.Defs.Contains(this))
                throw new NotImplementedException();

            variable.Uses.Remove(this);
            variable.Defs.Remove(this);
        }
    }
}