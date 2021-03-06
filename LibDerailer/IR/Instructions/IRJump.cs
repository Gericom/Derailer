﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.IR.Expressions;

namespace LibDerailer.IR.Instructions
{
    public class IRJump : IRInstruction
    {
        private IRExpression _condition;

        /// <summary>
        /// Basic block that is the destination of this jump
        /// </summary>
        public IRBasicBlock Destination { get; set; }

        /// <summary>
        /// Condition for taking this jump, null if unconditional
        /// </summary>
        public IRExpression Condition
        {
            get => _condition;
            set
            {
                _condition = value;
                Uses.Clear();
                if (!(_condition is null))
                    Uses.UnionWith(_condition.GetAllVariables());
            }
        }
        
        public bool IsLoopJump { get; set; }

        public IRJump(IRBasicBlock parentBlock, IRBasicBlock destination, IRExpression condition)
            : base(parentBlock)
        {
            Destination = destination;
            Condition   = condition;
        }

        public override void SubstituteUse(IRVariable variable, IRExpression expression)
        {
            if (Condition is null)
                return;

            if (ReferenceEquals(Condition, variable))
                Condition = expression.CloneComplete();
            else
                Condition.Substitute(variable, expression);

            Uses.Clear();
            if (!(_condition is null))
                Uses.UnionWith(_condition.GetAllVariables());
        }

        public override void SubstituteDef(IRVariable variable, IRExpression expression)
        {
            
        }

        public override void Substitute(IRExpression template, IRExpression substitution, IRExpression.OnMatchFoundHandler callback)
        {
            if (Condition is null)
                return;

            Condition.Substitute(template, substitution, callback);
            var mapping = new Dictionary<IRVariable, IRExpression>();
            if (Condition.Unify(template, mapping) && callback(mapping))
            {
                if (substitution is IRVariable v)
                    Condition = mapping[v].CloneComplete();
                else
                {
                    var newExpr = substitution.CloneComplete();
                    foreach (var varMap in mapping)
                        newExpr.Substitute(varMap.Key, varMap.Value);
                    Condition = newExpr;
                }
            }
        }
    }
}