using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.CCodeGen.Statements.Expressions;
using LibDerailer.IR.Instructions;
using LibDerailer.IR.Types;

namespace LibDerailer.IR.Expressions
{
    public abstract class IRVariable : IRExpression
    {
        public string Name { get; set; }

        public IRVariable(IRType type, string name)
            : base(type)
        {
            if (!(type is IRMatchType) && type == IRPrimitive.Void)
                throw new IRTypeException();
            Name = name;
        }

        public override IRExpression CloneComplete()
            => this;

        public override HashSet<IRVariable> GetAllVariables()
            => new HashSet<IRVariable>(new[] {this});

        public override CExpression ToCExpression()
            => new CVariable(Name);

        public override string ToString() => Name;

        public override bool Unify(IRExpression template, Dictionary<IRVariable, IRExpression> varMapping)
        {
            if (!(template is IRVariable exp) || !exp.Type.Equals(Type))
                return false;
            if (varMapping.ContainsKey(exp))
                return varMapping[exp].Equals(this);
            varMapping[exp] = this;
            return true;
        }

        public override void Substitute(IRExpression template, IRExpression substitution, OnMatchFoundHandler callback)
        {
            
        }

        public override void Substitute(IRVariable variable, IRExpression expression)
        {
            
        }

        public override bool Equals(object obj)
            => ReferenceEquals(this, obj);
    }
}