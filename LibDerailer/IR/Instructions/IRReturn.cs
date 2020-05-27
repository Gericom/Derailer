using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.CCodeGen.Statements;
using LibDerailer.IR.Expressions;

namespace LibDerailer.IR.Instructions
{
    public class IRReturn : IRInstruction
    {
        public IRExpression ReturnValue { get; set; }

        public IRReturn(IRBasicBlock parentBlock, IRExpression returnValue)
            : base(parentBlock)
        {
            ReturnValue = returnValue;
        }

        public override IEnumerable<CStatement> ToCCode()
        {
            yield return new CReturn(ReturnValue?.ToCExpression());
        }

        public override void Substitute(IRVariable variable, IRExpression expression)
        {
            if (ReferenceEquals(ReturnValue, variable))
                ReturnValue = expression;
            else
                ReturnValue.Substitute(variable, expression);

            if (variable.Uses.Contains(this))
                foreach (var v in expression.GetAllVariables())
                    v.Uses.Add(this);

            variable.Uses.Remove(this);
        }
    }
}