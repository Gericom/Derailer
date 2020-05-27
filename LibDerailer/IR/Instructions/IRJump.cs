using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.IR.Expressions;

namespace LibDerailer.IR.Instructions
{
    public class IRJump : IRInstruction
    {
        /// <summary>
        /// Basic block that is the destination of this jump
        /// </summary>
        public IRBasicBlock Destination { get; }

        /// <summary>
        /// Condition for taking this jump, null if unconditional
        /// </summary>
        public IRExpression Condition { get; set; }

        public IRJump(IRBasicBlock parentBlock, IRBasicBlock destination, IRExpression condition)
            : base(parentBlock)
        {
            Destination = destination;
            Condition   = condition;
        }

        public override void Substitute(IRVariable variable, IRExpression expression)
        {
            if (ReferenceEquals(Condition, variable))
                Condition = expression;
            else
                Condition.Substitute(variable, expression);

            if (variable.Uses.Contains(this))
                foreach (var v in expression.GetAllVariables())
                    v.Uses.Add(this);

            variable.Uses.Remove(this);
        }
    }
}