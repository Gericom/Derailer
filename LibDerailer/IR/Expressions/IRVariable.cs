using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.CCodeGen.Statements.Expressions;
using LibDerailer.IR.Instructions;

namespace LibDerailer.IR.Expressions
{
    public abstract class IRVariable : IRExpression
    {
        public string Name { get; set; }

        // public HashSet<IRInstruction> Defs { get; } = new HashSet<IRInstruction>();
        // public HashSet<IRInstruction> Uses { get; } = new HashSet<IRInstruction>();

        public IRVariable(IRType type, string name)
            : base(type)
        {
            if (type == IRType.Void)
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

        public override bool Equals(object obj)
            => ReferenceEquals(this, obj);
    }
}