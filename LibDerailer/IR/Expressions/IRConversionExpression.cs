using System;
using System.Collections.Generic;
using LibDerailer.CCodeGen.Statements.Expressions;
using LibDerailer.IR.Types;

namespace LibDerailer.IR.Expressions
{
    public class IRConversionExpression : IRExpression
    {
        private IRExpression _operand;

        public IRExpression Operand
        {
            get => _operand;
            set
            {
                if (!(value.Type is IRPrimitive opPrim))
                    throw new IRTypeException();
                _operand = value;
            }
        }

        public IRConversionExpression(IRType type, IRExpression operand)
            : base(type)
        {
            if (!(type is IRPrimitive primType) || !(operand.Type is IRPrimitive opPrim))
                throw new IRTypeException();

            Operand  = operand;
        }

        public override IRExpression CloneComplete()
            => new IRConversionExpression(Type, Operand.CloneComplete());

        public override void Substitute(IRVariable variable, IRExpression expression)
        {
            if (ReferenceEquals(Operand, variable))
                Operand = expression.CloneComplete();
            else
                Operand.Substitute(variable, expression);
        }

        public override HashSet<IRVariable> GetAllVariables()
            => Operand.GetAllVariables();

        public override CExpression ToCExpression()
            => new CCast(Type.ToCType(), Operand.ToCExpression());

        public override bool Equals(object obj)
            => obj is IRConversionExpression exp &&
               exp.Type == Type &&
               exp.Operand.Equals(Operand);
    }
}