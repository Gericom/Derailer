using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.CCodeGen;
using LibDerailer.CCodeGen.Statements;
using LibDerailer.CCodeGen.Statements.Expressions;
using LibDerailer.IR.Expressions;
using LibDerailer.IR.Types;

namespace LibDerailer.IR.Instructions
{
    public class IRStore : IRInstruction
    {
        public IRType       Type    { get; }
        public IRExpression Address { get; set; }
        public IRExpression Operand { get; set; }


        public IRStore(IRBasicBlock parentBlock, IRType type, IRExpression address, IRExpression operand)
            : base(parentBlock)
        {
            if (type == IRPrimitive.Void || type == IRPrimitive.Bool)
                throw new IRTypeException();
            Type     = type;
            Address  = address;
            Operand  = operand;
            Uses.UnionWith(Address.GetAllVariables());
            Uses.UnionWith(Operand.GetAllVariables());
        }

        public override IEnumerable<CStatement> ToCCode()
        {
            yield return CExpression.Assign(
                CExpression.Deref(new CCast(new CType(Type.ToCType(), true), Address.ToCExpression())),
                Operand.ToCExpression());
        }

        public override void SubstituteUse(IRVariable variable, IRExpression expression)
        {
            if (ReferenceEquals(Address, variable))
                Address = expression.CloneComplete();
            else
                Address.Substitute(variable, expression);

            Uses.Clear();
            Uses.UnionWith(Address.GetAllVariables());

            if (ReferenceEquals(Operand, variable))
                Operand = expression.CloneComplete();
            else
                Operand.Substitute(variable, expression);

            Uses.UnionWith(Operand.GetAllVariables());
        }

        public override void SubstituteDef(IRVariable variable, IRExpression expression)
        {
            
        }

        public override void Substitute(IRExpression template, IRExpression substitution, IRExpression.OnMatchFoundHandler callback)
        {
            Operand.Substitute(template, substitution, callback);
            var mapping = new Dictionary<IRVariable, IRExpression>();
            if (Operand.Unify(template, mapping) && callback(mapping))
            {
                var newExpr = substitution.CloneComplete();
                foreach (var varMap in mapping)
                    newExpr.Substitute(varMap.Key, varMap.Value);
                Operand = newExpr;
            }
        }
    }
}