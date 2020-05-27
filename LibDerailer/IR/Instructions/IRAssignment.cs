using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.CCodeGen.Statements;
using LibDerailer.CCodeGen.Statements.Expressions;
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
        }

        public override IEnumerable<CStatement> ToCCode()
        {
            yield return CExpression.Assign(Destination.ToCExpression(), Source.ToCExpression());
        }

        public override void Substitute(IRVariable variable, IRExpression expression)
        {
            if (ReferenceEquals(Destination, variable))
                Destination = expression;
            else
                Destination.Substitute(variable, expression);

            if (ReferenceEquals(Source, variable))
                Source = expression;
            else
                Source.Substitute(variable, expression);

            if(variable.Uses.Contains(this))
                foreach (var v in expression.GetAllVariables())
                    v.Uses.Add(this);

            if (variable.Defs.Contains(this))
                throw new NotImplementedException();

            variable.Uses.Remove(this);
            variable.Defs.Remove(this);
        }
    }
}