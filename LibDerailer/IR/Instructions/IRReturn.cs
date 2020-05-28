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
            if(!(ReturnValue is null))
                Uses.UnionWith(ReturnValue.GetAllVariables());
        }

        public override IEnumerable<CStatement> ToCCode()
        {
            yield return new CReturn(ReturnValue?.ToCExpression());
        }

        public override void SubstituteUse(IRVariable variable, IRExpression expression)
        {
            if (ReferenceEquals(ReturnValue, variable))
                ReturnValue = expression.CloneComplete();
            else
                ReturnValue.Substitute(variable, expression);

            Uses.Clear();
            Uses.UnionWith(ReturnValue.GetAllVariables());
        }

        public override void SubstituteDef(IRVariable variable, IRExpression expression)
        {
            
        }
    }
}