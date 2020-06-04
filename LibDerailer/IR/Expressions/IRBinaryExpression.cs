using System;
using System.Collections.Generic;
using LibDerailer.CCodeGen.Statements.Expressions;
using LibDerailer.IR.Types;

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

        public override IRExpression CloneComplete()
            => new IRBinaryExpression(Type, Operator, OperandA.CloneComplete(), OperandB.CloneComplete());

        public override void Substitute(IRVariable variable, IRExpression expression)
        {
            if (ReferenceEquals(OperandA, variable))
                OperandA = expression.CloneComplete();
            else
                OperandA.Substitute(variable, expression);

            if (ReferenceEquals(OperandB, variable))
                OperandB = expression.CloneComplete();
            else
                OperandB.Substitute(variable, expression);
        }

        public override void Substitute(IRExpression template, IRExpression substitution, OnMatchFoundHandler callback)
        {
            OperandA.Substitute(template, substitution, callback);
            var mapping = new Dictionary<IRVariable, IRExpression>();
            if (OperandA.Unify(template, mapping) && callback(mapping))
            {
                var newExpr = substitution.CloneComplete();
                foreach (var varMap in mapping)
                    newExpr.Substitute(varMap.Key, varMap.Value);
                OperandA = newExpr;
            }

            OperandB.Substitute(template, substitution, callback);
            mapping = new Dictionary<IRVariable, IRExpression>();
            if (OperandB.Unify(template, mapping) && callback(mapping))
            {
                var newExpr = substitution.CloneComplete();
                foreach (var varMap in mapping)
                    newExpr.Substitute(varMap.Key, varMap.Value);
                OperandB = newExpr;
            }
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
                    if (Type == IRPrimitive.Bool)
                        return OperandA.ToCExpression().BooleanAnd(OperandB.ToCExpression());
                    return OperandA.ToCExpression() & OperandB.ToCExpression();
                case IRBinaryOperator.Or:
                    if (Type == IRPrimitive.Bool)
                        return OperandA.ToCExpression().BooleanOr(OperandB.ToCExpression());
                    return OperandA.ToCExpression() | OperandB.ToCExpression();
                case IRBinaryOperator.Xor:
                    return OperandA.ToCExpression() ^ OperandB.ToCExpression();
                case IRBinaryOperator.Lsl:
                    return OperandA.ToCExpression().ShiftLeft(OperandB.ToCExpression());
                case IRBinaryOperator.Lsr:
                    return new CCast(((IRPrimitive) OperandA.Type).ToUnsigned().ToCType(), OperandA.ToCExpression())
                        .ShiftRight(OperandB.ToCExpression());
                case IRBinaryOperator.Asr:
                    return new CCast(((IRPrimitive) OperandA.Type).ToSigned().ToCType(), OperandA.ToCExpression())
                        .ShiftRight(OperandB.ToCExpression());
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

        public override bool Unify(IRExpression template, Dictionary<IRVariable, IRExpression> varMapping)
        {
            if (template is IRVariable templateVar && templateVar.Type == Type)
            {
                if (varMapping.ContainsKey(templateVar))
                    return varMapping[templateVar].Equals(this);
                varMapping[templateVar] = this;
                return true;
            }

            if (!(template is IRBinaryExpression exp) || exp.Operator != Operator || !exp.Type.Equals(Type))
                return false;
            return OperandA.Unify(exp.OperandA, varMapping) && OperandB.Unify(exp.OperandB, varMapping);
        }

        public override bool Equals(object obj)
            => obj is IRBinaryExpression exp &&
               exp.Operator == Operator &&
               exp.Type == Type &&
               exp.OperandA.Equals(OperandA) &&
               exp.OperandB.Equals(OperandB);
    }
}