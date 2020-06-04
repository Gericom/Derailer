using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.CCodeGen.Statements.Expressions;
using LibDerailer.IR.Types;

namespace LibDerailer.IR.Expressions
{
    public class IRCallExpression : IRExpression
    {
        public string             TargetName { get; set; }
        public List<IRExpression> Arguments  { get; } = new List<IRExpression>();

        public IRCallExpression(IRType returnType, string targetName, params IRExpression[] arguments)
            : base(returnType)

        {
            TargetName = targetName;
            Arguments.AddRange(arguments);
        }

        public override IRExpression CloneComplete()
            => new IRCallExpression(Type, TargetName, Arguments.Select(a => a.CloneComplete()).ToArray());

        public override void Substitute(IRVariable variable, IRExpression expression)
        {
            for (int i = 0; i < Arguments.Count; i++)
            {
                if (ReferenceEquals(Arguments[i], variable))
                    Arguments[i] = expression.CloneComplete();
                else
                    Arguments[i].Substitute(variable, expression);
            }
        }

        public override void Substitute(IRExpression template, IRExpression substitution, OnMatchFoundHandler callback)
        {
            for(int i = 0; i < Arguments.Count; i++)
            {
                Arguments[i].Substitute(template, substitution, callback);
                var mapping = new Dictionary<IRVariable, IRExpression>();
                if (Arguments[i].Unify(template, mapping) && callback(mapping))
                {
                    if (substitution is IRVariable v)
                        Arguments[i] = mapping[v].CloneComplete();
                    else
                    {
                        var newExpr = substitution.CloneComplete();
                        foreach (var varMap in mapping)
                            newExpr.Substitute(varMap.Key, varMap.Value);
                        Arguments[i] = newExpr;
                    }
                }
            }
        }

        public override HashSet<IRVariable> GetAllVariables()
        {
            var vars = new HashSet<IRVariable>();
            foreach (var argument in Arguments)
                vars.UnionWith(argument.GetAllVariables());
            return vars;
        }

        public override CExpression ToCExpression()
            => new CMethodCall(false, TargetName, Arguments.Select(a => a.ToCExpression()).ToArray());

        public override bool Unify(IRExpression template, Dictionary<IRVariable, IRExpression> varMapping)
        {
            if (template is IRVariable templateVar && templateVar.Type == Type)
            {
                if (varMapping.ContainsKey(templateVar))
                    return varMapping[templateVar].Equals(this);
                varMapping[templateVar] = this;
                return true;
            }

            if (!(template is IRCallExpression exp) || exp.TargetName != TargetName || 
                !exp.Type.Equals(Type) || exp.Arguments.Count != Arguments.Count)
                return false;
            for(int i = 0; i < Arguments.Count; i++)
                if (!Arguments[i].Unify(exp.Arguments[i], varMapping))
                    return false;
            return true;
        }

        public override bool Equals(object obj)
            => obj is IRCallExpression exp &&
               exp.TargetName == TargetName &&
               exp.Type == Type &&
               exp.Arguments.SequenceEqual(Arguments);
    }
}