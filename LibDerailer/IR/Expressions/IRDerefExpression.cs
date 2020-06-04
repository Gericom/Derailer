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

        public override void Substitute(IRExpression template, IRExpression substitution, OnMatchFoundHandler callback)
        {
            Pointer.Substitute(template, substitution, callback);
            var mapping = new Dictionary<IRVariable, IRExpression>();
            if (Pointer.Unify(template, mapping) && callback(mapping))
            {
                if (substitution is IRVariable v)
                    Pointer = mapping[v].CloneComplete();
                else
                {
                    var newExpr = substitution.CloneComplete();
                    foreach (var varMap in mapping)
                        newExpr.Substitute(varMap.Key, varMap.Value);
                    Pointer = newExpr;
                }
            }
        }

        public override IRExpression CloneComplete()
            => new IRDerefExpression(Type, Pointer.CloneComplete());

        public override CExpression ToCExpression()
            => CExpression.Deref(new CCast(new CType(Type.ToCType(), true), Pointer.ToCExpression()));

        public override bool Unify(IRExpression template, Dictionary<IRVariable, IRExpression> varMapping)
        {
            if (template is IRVariable templateVar && templateVar.Type == Type)
            {
                if (varMapping.ContainsKey(templateVar))
                    return varMapping[templateVar].Equals(this);
                varMapping[templateVar] = this;
                return true;
            }

            if (!(template is IRDerefExpression exp) || !exp.Type.Equals(Type))
                return false;
            return Pointer.Unify(exp.Pointer, varMapping);
        }

        public override bool Equals(object obj)
            => obj is IRDerefExpression exp &&
               exp.Type == Type &&
               exp.Pointer.Equals(Pointer);
    }
}