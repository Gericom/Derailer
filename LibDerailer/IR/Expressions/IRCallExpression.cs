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

        public override HashSet<IRVariable> GetAllVariables()
        {
            var vars = new HashSet<IRVariable>();
            foreach (var argument in Arguments)
                vars.UnionWith(argument.GetAllVariables());
            return vars;
        }

        public override CExpression ToCExpression()
            => new CMethodCall(false, TargetName, Arguments.Select(a => a.ToCExpression()).ToArray());

        public override bool Equals(object obj)
            => obj is IRCallExpression exp &&
               exp.TargetName == TargetName &&
               exp.Type == Type &&
               exp.Arguments.SequenceEqual(Arguments);
    }
}