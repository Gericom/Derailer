using System;
using System.Collections.Generic;
using LibDerailer.CCodeGen.Statements.Expressions;

namespace LibDerailer.IR.Expressions
{
    public class IRBinaryExpression : IRExpression
    {
        public IRBinaryOperator Operator { get; }

        private IRExpression _operandA;

        public IRExpression OperandA
        {
            get => _operandA;
            set
            {
                if (value.Type != Type)
                    throw new IRTypeException();
                _operandA = value;
            }
        }

        private IRExpression _operandB;

        public IRExpression OperandB
        {
            get => _operandB;
            set
            {
                if (value.Type != Type)
                    throw new IRTypeException();
                _operandB = value;
            }
        }

        public IRBinaryExpression(IRType type, IRBinaryOperator op, IRExpression operandA, IRExpression operandB)
            : base(type)
        {
            if (operandA.Type != type)
                throw new IRTypeException();
            if (operandB.Type != type)
                throw new IRTypeException();

            Operator = op;
            OperandA = operandA;
            OperandB = operandB;
        }

        public override void Substitute(IRVariable variable, IRExpression expression)
        {
            if (ReferenceEquals(OperandA, variable))
                OperandA = expression;
            else
                OperandA.Substitute(variable, expression);

            if (ReferenceEquals(OperandB, variable))
                OperandB = expression;
            else
                OperandB.Substitute(variable, expression);
        }

        public override HashSet<IRVariable> GetAllVariables()
        {
            var vars = new HashSet<IRVariable>();
            vars.UnionWith(OperandA.GetAllVariables());
            vars.UnionWith(OperandB.GetAllVariables());
            return vars;
        }

        public override CExpression ToCExpression()
        {
            switch (Operator)
            {
                case IRBinaryOperator.Add:
                    return OperandA.ToCExpression() + OperandB.ToCExpression();
                case IRBinaryOperator.Sub:
                    return OperandA.ToCExpression() - OperandB.ToCExpression();
                case IRBinaryOperator.And:
                    if (Type == IRType.I1)
                        return OperandA.ToCExpression().BooleanAnd(OperandB.ToCExpression());
                    return OperandA.ToCExpression() & OperandB.ToCExpression();
                case IRBinaryOperator.Or:
                    if (Type == IRType.I1)
                        return OperandA.ToCExpression().BooleanOr(OperandB.ToCExpression());
                    return OperandA.ToCExpression() | OperandB.ToCExpression();
                case IRBinaryOperator.Xor:
                    return OperandA.ToCExpression() ^ OperandB.ToCExpression();
                case IRBinaryOperator.Lsl:
                    return OperandA.ToCExpression().ShiftLeft(OperandB.ToCExpression());
                case IRBinaryOperator.Lsr:
                    return OperandA.ToCExpression().ShiftRight(OperandB.ToCExpression());
                case IRBinaryOperator.Asr:
                    return new CCast(OperandA.Type.ToCType(true), OperandA.ToCExpression()).ShiftRight(OperandB.ToCExpression());
                case IRBinaryOperator.Mul:
                    return OperandA.ToCExpression() * OperandB.ToCExpression();
                case IRBinaryOperator.Div:
                    return OperandA.ToCExpression() / OperandB.ToCExpression();
                case IRBinaryOperator.Mod:
                    return OperandA.ToCExpression() % OperandB.ToCExpression();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}