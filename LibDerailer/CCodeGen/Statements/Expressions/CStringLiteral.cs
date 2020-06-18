using System.Collections.Generic;

namespace LibDerailer.CCodeGen.Statements.Expressions
{
    public class CStringLiteral : CLiteral
    {
        public string Value { get; set; }

        public CStringLiteral(string v)
        {
            Value = v;
        }

        public override string ToString() => $"\"{Value}\"";
        public override IEnumerable<CToken> ToTokens()
        {
            yield return new CToken(CTokenType.Literal, ToString());
        }
    };
}