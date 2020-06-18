using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.CCodeGen.Statements.Expressions;

namespace LibDerailer.CCodeGen.Statements
{
    public class CReturn : CStatement
    {
        public CExpression ReturnVal { get; set; }

        public CReturn(CExpression returnVal = null)
        {
            ReturnVal = returnVal;
        }

        public override string ToString() => !(ReturnVal is null) ? $"return {ReturnVal};" : "return;";

        public override IEnumerable<CToken> ToTokens()
        {
            yield return new CToken(CTokenType.Keyword, "return");

            if (!(ReturnVal is null))
            {
                yield return new CToken(CTokenType.Whitespace, " ");
                foreach (var tok in ReturnVal.ToTokens())
                    yield return tok;
            }

            yield return new CToken(CTokenType.Semicolon, ";");
        }
    }
}
