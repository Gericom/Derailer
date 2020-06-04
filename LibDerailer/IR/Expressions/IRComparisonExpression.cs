using System;
using System.Collections.Generic;
using LibDerailer.CCodeGen.Statements.Expressions;
using LibDerailer.IR.Types;

namespace LibDerailer.IR.Expressions
{
    public class IRComparisonExpression : IRExpression
    {
        public IRComparisonOperator Operator { get; }

        private IRExpression _operandA;

        public IRExpression OperandA
        {
            get => _operandA;
            set
            {
                if (!(OperandB is null) && value.Type != OperandB.Type)
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
                if (!(OperandA is null) && value.Type != OperandA.Type)
                    throw new IRTypeException();
                _operandB = value;
            }
        }

        public IRComparisonExpression(IRComparisonOperator op, IRExpression operandA, IRExpression operandB)
            : base(IRPrimitive.Bool)
        {
            if (operandA.Type != operandB.Type)
                throw new IRTypeException();

            Operator = op;
            OperandA = operandA;
            OperandB = operandB;
        }

        public override IRExpression CloneComplete()
            => new IRComparisonExpression(Operator, OperandA.CloneComplete(), OperandB.CloneComplete());

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
                if (substitution is IRVariable v)
                    OperandA = mapping[v].CloneComplete();
                else
                {
                    var newExpr = substitution.CloneComplete();
                    foreach (var varMap in mapping)
                        newExpr.Substitute(varMap.Key, varMap.Value);
                    OperandA = newExpr;
                }
            }

            OperandB.Substitute(template, substitution, callback);
            mapping = new Dictionary<IRVariable, IRExpression>();
            if (OperandB.Unify(template, mapping) && callback(mapping))
            {
                if (substitution is IRVariable v)
                    OperandB = mapping[v].CloneComplete();
                else
                {
                    var newExpr = substitution.CloneComplete();
                    foreach (var varMap in mapping)
                        newExpr.Substitute(varMap.Key, varMap.Value);
                    OperandB = newExpr;
                }
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
                case IRComparisonOperator.Equal:
                    return OperandA.ToCExpression() == OperandB.ToCExpression();
                case IRComparisonOperator.NotEqual:
                    return OperandA.ToCExpression() != OperandB.ToCExpression();
                case IRComparisonOperator.Less:
                    return new CCast(((IRPrimitive) OperandA.Type).ToSigned().ToCType(), OperandA.ToCExpression()) <
                           new CCast(((IRPrimitive) OperandB.Type).ToSigned().ToCType(), OperandB.ToCExpression());
                case IRComparisonOperator.LessEqual:
                    return new CCast(((IRPrimitive) OperandA.Type).ToSigned().ToCType(), OperandA.ToCExpression()) <=
                           new CCast(((IRPrimitive) OperandB.Type).ToSigned().ToCType(), OperandB.ToCExpression());
                case IRComparisonOperator.Greater:
                    return new CCast(((IRPrimitive) OperandA.Type).ToSigned().ToCType(), OperandA.ToCExpression()) >
                           new CCast(((IRPrimitive) OperandB.Type).ToSigned().ToCType(), OperandB.ToCExpression());
                case IRComparisonOperator.GreaterEqual:
                    return new CCast(((IRPrimitive) OperandA.Type).ToSigned().ToCType(), OperandA.ToCExpression()) <=
                           new CCast(((IRPrimitive) OperandB.Type).ToSigned().ToCType(), OperandB.ToCExpression());
                case IRComparisonOperator.UnsignedLess:
                    return new CCast(((IRPrimitive) OperandA.Type).ToUnsigned().ToCType(), OperandA.ToCExpression()) <
                           new CCast(((IRPrimitive) OperandB.Type).ToUnsigned().ToCType(), OperandB.ToCExpression());
                case IRComparisonOperator.UnsignedLessEqual:
                    return new CCast(((IRPrimitive) OperandA.Type).ToUnsigned().ToCType(), OperandA.ToCExpression()) <=
                           new CCast(((IRPrimitive) OperandB.Type).ToUnsigned().ToCType(), OperandB.ToCExpression());
                case IRComparisonOperator.UnsignedGreater:
                    return new CCast(((IRPrimitive) OperandA.Type).ToUnsigned().ToCType(), OperandA.ToCExpression()) >
                           new CCast(((IRPrimitive) OperandB.Type).ToUnsigned().ToCType(), OperandB.ToCExpression());
                case IRComparisonOperator.UnsignedGreaterEqual:
                    return new CCast(((IRPrimitive) OperandA.Type).ToUnsigned().ToCType(), OperandA.ToCExpression()) >=
                           new CCast(((IRPrimitive) OperandB.Type).ToUnsigned().ToCType(), OperandB.ToCExpression());
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

            if (!(template is IRComparisonExpression exp) || exp.Operator != Operator || !exp.Type.Equals(Type))
                return false;
            return OperandA.Unify(exp.OperandA, varMapping) && OperandB.Unify(exp.OperandB, varMapping);
        }

        public override bool Equals(object obj)
            => obj is IRComparisonExpression exp &&
               exp.Operator == Operator &&
               exp.Type == Type &&
               exp.OperandA.Equals(OperandA) &&
               exp.OperandB.Equals(OperandB);
    }
}