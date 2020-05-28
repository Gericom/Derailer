using System.Collections.Generic;
using System.Linq;
using LibDerailer.CCodeGen.Statements;
using LibDerailer.IR.Expressions;

namespace LibDerailer.IR.Instructions
{
    public abstract class IRInstruction
    {
        public IRBasicBlock ParentBlock { get; }

        public HashSet<IRVariable> Uses { get; } = new HashSet<IRVariable>();
        public HashSet<IRVariable> Defs { get; } = new HashSet<IRVariable>();

        public HashSet<IRVariable> LiveIns  { get; } = new HashSet<IRVariable>();
        public HashSet<IRVariable> LiveOuts { get; } = new HashSet<IRVariable>();

        public HashSet<IRVariable> Dead { get; } = new HashSet<IRVariable>();

        protected IRInstruction(IRBasicBlock parentBlock)
        {
            ParentBlock = parentBlock;
        }

        public virtual IEnumerable<CStatement> ToCCode()
            => Enumerable.Empty<CStatement>();

        public abstract void SubstituteUse(IRVariable variable, IRExpression expression);

        public abstract void SubstituteDef(IRVariable variable, IRExpression expression);
    }
}