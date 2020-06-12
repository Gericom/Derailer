using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.CCodeGen.Statements.Expressions;
using LibDerailer.IR.Types;

namespace LibDerailer.IR.Expressions
{
    public class IRStructFieldExpression : IRExpression
    {
        public IRExpression           BasePointer { get; set; }
        public IRStruct.IRStructField StructField { get; }
        public IRExpression           Index       { get; set; }

        public IRStructFieldExpression(IRExpression basePointer, IRStruct.IRStructField structField, IRExpression index)
            : base(structField.Type)
        {
            if (!(basePointer.Type.GetRootType() is IRStruct s1 && s1.Fields.Contains(structField)) &&
                !(basePointer.Type.GetRootType() is IRPointer p && p.BaseType.GetRootType() is IRStruct s2 &&
                  s2.Fields.Contains(structField)))
                throw new IRTypeException();
            BasePointer = basePointer;
            StructField = structField;
            Index       = index;
        }

        public override HashSet<IRVariable> GetAllVariables()
        {
            var vars = new HashSet<IRVariable>();
            vars.UnionWith(BasePointer.GetAllVariables());
            if(!(Index is null))
                vars.UnionWith(Index.GetAllVariables());
            return vars;
        }

        public override void Substitute(IRVariable variable, IRExpression expression)
        {
            if (ReferenceEquals(BasePointer, variable))
                BasePointer = expression.CloneComplete();
            else
                BasePointer.Substitute(variable, expression);

            if (Index is null)
                return;

            if (ReferenceEquals(Index, variable))
                Index = expression.CloneComplete();
            else
                Index.Substitute(variable, expression);
        }

        public override IRExpression CloneComplete()
            => new IRStructFieldExpression(BasePointer.CloneComplete(), StructField, Index?.CloneComplete());

        public override bool Unify(IRExpression template, Dictionary<IRVariable, IRExpression> varMapping)
        {
            if (template is IRVariable templateVar && templateVar.Type == Type)
            {
                if (varMapping.ContainsKey(templateVar))
                    return varMapping[templateVar].Equals(this);
                varMapping[templateVar] = this;
                return true;
            }

            if (!(template is IRStructFieldExpression exp) || !exp.BasePointer.Equals(BasePointer) ||
                !exp.Index.Equals(Index) || exp.StructField != StructField || !exp.Type.Equals(Type))
                return false;
            return BasePointer.Unify(exp.BasePointer, varMapping) &&
                   (Index is null || Index.Unify(exp.Index, varMapping));
        }

        public override void Substitute(IRExpression template, IRExpression substitution, OnMatchFoundHandler callback)
        {
            BasePointer.Substitute(template, substitution, callback);
            var mapping = new Dictionary<IRVariable, IRExpression>();
            if (BasePointer.Unify(template, mapping) && callback(mapping))
            {
                if (substitution is IRVariable v)
                    BasePointer = mapping[v].CloneComplete();
                else
                {
                    var newExpr = substitution.CloneComplete();
                    foreach (var varMap in mapping)
                        newExpr.Substitute(varMap.Key, varMap.Value);
                    BasePointer = newExpr;
                }
            }

            if (Index is null)
                return;

            Index.Substitute(template, substitution, callback);
            mapping = new Dictionary<IRVariable, IRExpression>();
            if (Index.Unify(template, mapping) && callback(mapping))
            {
                if (substitution is IRVariable v)
                    Index = mapping[v].CloneComplete();
                else
                {
                    var newExpr = substitution.CloneComplete();
                    foreach (var varMap in mapping)
                        newExpr.Substitute(varMap.Key, varMap.Value);
                    Index = newExpr;
                }
            }
        }

        public override CExpression ToCExpression()
        {
            if(BasePointer.Type.GetRootType() is IRPointer)
                return new CMethodCall(true, "->", BasePointer.ToCExpression(), new CRawLiteral<string>(StructField.Name));
            else
                return new CMethodCall(true, ".", BasePointer.ToCExpression(), new CRawLiteral<string>(StructField.Name));
        }
    }
}