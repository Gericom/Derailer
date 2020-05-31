using System;
using System.Collections.Generic;
using LibDerailer.CCodeGen.Statements.Expressions;

namespace LibDerailer.IR.Expressions
{
    public class IRConversionExpression : IRExpression
    {
        public IRConversionOperator Operator { get; }

        private IRExpression _operand;

        public IRExpression Operand
        {
            get => _operand;
            set
            {
                if (Operator == IRConversionOperator.Trunc && value.Type < Type)
                    throw new IRTypeException();
                if (Operator != IRConversionOperator.Trunc && value.Type > Type)
                    throw new IRTypeException();
                _operand = value;
            }
        }

        public IRConversionExpression(IRType type, IRConversionOperator op, IRExpression operand)
            : base(type)
        {
            if (op == IRConversionOperator.Trunc && operand.Type < type)
                throw new IRTypeException();
            if (op != IRConversionOperator.Trunc && operand.Type > type)
                throw new IRTypeException();

            Operator = op;
            Operand  = operand;
        }

        public override IRExpression CloneComplete()
            => new IRConversionExpression(Type, Operator, Operand.CloneComplete());

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
        {
            switch (Operator)
            {
                case IRConversionOperator.Sext:
                    return new CCast(Type.ToCType(true), Operand.ToCExpression());
                case IRConversionOperator.Zext:
                case IRConversionOperator.Trunc:
                    return new CCast(Type.ToCType(false), Operand.ToCExpression());
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override bool Equals(object obj)
            => obj is IRConversionExpression exp &&
               exp.Operator == Operator &&
               exp.Type == Type &&
               exp.Operand.Equals(Operand);
    }
}