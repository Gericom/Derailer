using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.CCodeGen;
using LibDerailer.CCodeGen.Statements.Expressions;
using LibDerailer.IR.Types;

namespace LibDerailer.IR.Expressions
{
    public class IRDerefExpression : IRExpression
    {
        public IRExpression Pointer { get; set; }

        public IRDerefExpression(IRType type, IRExpression pointer)
            : base(type)
        {
            Pointer = pointer;
        }

        public override HashSet<IRVariable> GetAllVariables()
            => Pointer.GetAllVariables();

        public override void Substitute(IRVariable variable, IRExpression expression)
        {
            if (ReferenceEquals(Pointer, variable))
                Pointer = expression.CloneComplete();
            else
                Pointer.Substitute(variable, expression);
        }

        public override IRExpression CloneComplete()
            => new IRDerefExpression(Type, Pointer.CloneComplete());

        public override CExpression ToCExpression()
            => CExpression.Deref(new CCast(new CType(Type.ToCType(), true), Pointer.ToCExpression()));

        public override bool Equals(object obj)
            => obj is IRDerefExpression exp &&
               exp.Type == Type &&
               exp.Pointer.Equals(Pointer);
    }
}