using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDerailer.CCodeGen.Statements
{
    public class CContinue : CStatement
    {
        public override string ToString() => "continue;";

        public override IEnumerable<CToken> ToTokens()
        {
            yield return new CToken(CTokenType.Keyword, "continue");
            yield return new CToken(CTokenType.Semicolon, ";");
        }
    }
}