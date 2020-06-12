using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.CCodeGen.Statements;
using LibDerailer.CCodeGen.Statements.Expressions;
using LibDerailer.CodeGraph;
using LibDerailer.IR.Expressions;

namespace LibDerailer.IR.Instructions
{
    public class IRAssignment : IRInstruction
    {
        public IRExpression Destination { get; set; }
        public IRExpression Source      { get; set; }

        public IRAssignment(IRBasicBlock parentBlock, IRExpression destination, IRExpression source)
            : base(parentBlock)
        {
            Destination = destination;
            Source      = source;
            if (Destination is IRVariable v)
                Defs.Add(v);
            Uses.UnionWith(Source.GetAllVariables());
            if (!(Destination is IRVariable))
                Uses.UnionWith(Destination.GetAllVariables());
        }

        public override IEnumerable<CStatement> ToCCode()
        {
            yield return CExpression.Assign(Destination.ToCExpression(), Source.ToCExpression());
        }

        public override void SubstituteUse(IRVariable variable, IRExpression expression)
        {
            if (ReferenceEquals(Source, variable))
                Source = expression.CloneComplete();
            else
                Source.Substitute(variable, expression);

            Uses.Clear();
            Uses.UnionWith(Source.GetAllVariables());

            if(!(Destination is IRVariable))
            {
                Destination.Substitute(variable, expression);
                Uses.UnionWith(Destination.GetAllVariables());
            }
        }

        public override void SubstituteDef(IRVariable variable, IRExpression expression)
        {
            if (Destination is IRVariable)
            {
                if (ReferenceEquals(Destination, variable))
                    Destination = expression.CloneComplete();

                Defs.Clear();
                Defs.UnionWith(Destination.GetAllVariables());
            }
        }

        public override void Substitute(IRExpression template, IRExpression substitution, IRExpression.OnMatchFoundHandler callback)
        {
            Source.Substitute(template, substitution, callback);
            var mapping = new Dictionary<IRVariable, IRExpression>();
            if (Source.Unify(template, mapping) && callback(mapping))
            {
                if (substitution is IRVariable v)
                    Source = mapping[v].CloneComplete();
                else
                {
                    var newExpr = substitution.CloneComplete();
                    foreach (var varMap in mapping)
                        newExpr.Substitute(varMap.Key, varMap.Value);
                    Source = newExpr;
                }
            }
            if (!(Destination is IRVariable))
            {
                Destination.Substitute(template, substitution, callback);
                mapping = new Dictionary<IRVariable, IRExpression>();
                if (Destination.Unify(template, mapping) && callback(mapping))
                {
                    if (substitution is IRVariable v)
                        Destination = mapping[v].CloneComplete();
                    else
                    {
                        var newExpr = substitution.CloneComplete();
                        foreach (var varMap in mapping)
                            newExpr.Substitute(varMap.Key, varMap.Value);
                        Destination = newExpr;
                    }
                }
            }
        }
    }
}