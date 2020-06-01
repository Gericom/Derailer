using System;
using System.Collections.Generic;
using LibDerailer.CCodeGen.Statements.Expressions;
using LibDerailer.IR.Types;

namespace LibDerailer.IR.Expressions
{
    public class IRUnaryExpression : IRExpression
    {
        public IRUnaryOperator Operator { get; }

        private IRExpression _operand;

        public IRExpression Operand
        {
            get => _operand;
            set
            {
                if (value.Type != Type)
                    throw new IRTypeException();
                _operand = value;
            }
        }

        public IRUnaryExpression(IRType type, IRUnaryOperator op, IRExpression operand)
            : base(type)
        {
            if (operand.Type != type)
                throw new IRTypeException();

            Operator = op;
            Operand  = operand;
        }

        public override IRExpression CloneComplete()
            => new IRUnaryExpression(Type, Operator, Operand.CloneComplete());

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
                case IRUnaryOperator.Neg:
                    return -Operand.ToCExpression();
                case IRUnaryOperator.Not:
                    if (Type == IRPrimitive.Void)
                        return !Operand.ToCExpression();
                    return ~Operand.ToCExpression();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override bool Equals(object obj)
            => obj is IRUnaryExpression exp &&
               exp.Operator == Operator &&
               exp.Type == Type &&
               exp.Operand.Equals(Operand);
    }
}