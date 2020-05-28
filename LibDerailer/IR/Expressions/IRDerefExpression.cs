using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.CCodeGen;
using LibDerailer.CCodeGen.Statements.Expressions;

namespace LibDerailer.IR.Expressions
{
    public class IRDerefExpression : IRExpression
    {
        public IRExpression Pointer { get; set; }
        public bool IsSigned { get; set; }

        public IRDerefExpression(IRType type, bool isSigned, IRExpression pointer)
            : base(type)
        {
            IsSigned = isSigned;
            Pointer = pointer;
        }

        public override HashSet<IRVariable> GetAllVariables()
            => Pointer.GetAllVariables();

        public override void Substitute(IRVariable variable, IRExpression expression)
        {
            if (ReferenceEquals(Pointer, variable))
                Pointer = expression;
            else
                Pointer.Substitute(variable, expression);
        }

        public override IRExpression CloneComplete()
            => new IRDerefExpression(Type, IsSigned, Pointer.CloneComplete());

        public override CExpression ToCExpression()
            => CExpression.Deref(new CCast(new CType(Type.ToCType(IsSigned), true), Pointer.ToCExpression()));
    }
}