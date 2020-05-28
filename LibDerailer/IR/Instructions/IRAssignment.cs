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
        }

        public override void SubstituteDef(IRVariable variable, IRExpression expression)
        {
            if (expression is IRVariable)
            {
                if (ReferenceEquals(Destination, variable))
                    Destination = expression.CloneComplete();
                else
                    Destination.Substitute(variable, expression);

                Defs.Clear();
                Defs.UnionWith(Destination.GetAllVariables());
            }
        }
    }
}