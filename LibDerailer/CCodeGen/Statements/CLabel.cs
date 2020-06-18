using System.Collections.Generic;

namespace LibDerailer.CCodeGen.Statements
{
    public class CLabel : CStatement
    {
        public string Name { get; set; }

        public CLabel(string name)
        {
            Name = name;
        }

        public override string ToString() => $"{Name}:;";

        public override IEnumerable<CToken> ToTokens()
        {
            yield return new CToken(CTokenType.Identifier, Name);
            yield return new CToken(CTokenType.Colon, ":");
            yield return new CToken(CTokenType.Semicolon, ";");
        }
    }
}