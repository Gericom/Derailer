using System.Collections.Generic;
using System.Linq;
using LibDerailer.CCodeGen.Statements;
using LibDerailer.IR.Expressions;

namespace LibDerailer.IR.Instructions
{
    public abstract class IRInstruction
    {

        public IRBasicBlock ParentBlock { get; }

        protected IRInstruction(IRBasicBlock parentBlock)
        {
            ParentBlock = parentBlock;
        }

        public virtual IEnumerable<CStatement> ToCCode()
            => Enumerable.Empty<CStatement>();

        public virtual void Substitute(IRVariable variable, IRExpression expression)
        {
        }
    }
}