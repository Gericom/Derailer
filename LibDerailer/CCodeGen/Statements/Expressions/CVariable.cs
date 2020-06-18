using System.Collections.Generic;

namespace LibDerailer.CCodeGen.Statements.Expressions
{
    public class CVariable : CLiteral
    {
        public string Name { get; set; }

        public CVariable(string name)
        {
            Name = name;
        }

        public override string ToString() => Name;

        public override IEnumerable<CToken> ToTokens()
        {
            yield return new CToken(CTokenType.Identifier, Name);
        }

        public CMethodCall this[CExpression idx]
            => new CMethodCall(true, "[]", this, idx);
    }
}