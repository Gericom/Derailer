using System.Collections.Generic;

namespace LibDerailer.CCodeGen.Statements
{
    public class CGoto : CStatement
    {
        public string Label { get; set; }

        public CGoto(string label)
        {
            Label = label;
        }

        public override string ToString() 
            => $"goto {Label};";

        public override IEnumerable<CToken> ToTokens()
        {
            yield return new CToken(CTokenType.Keyword, "goto");
            yield return new CToken(CTokenType.Whitespace, " ");
            yield return new CToken(CTokenType.Identifier, Label);
            yield return new CToken(CTokenType.Semicolon, ";");
        }
    }
}