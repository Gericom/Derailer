using System.Collections.Generic;
using System.Linq;

namespace LibDerailer.CCodeGen
{
    public static class AstUtil
    {
        public static string Indent(string s) => s.Replace("\n", "\n    ");
        public static IEnumerable<CToken> Indent(IEnumerable<CToken> tokens) => (from token in tokens
                                                                                 select new CToken(token.Type, token.Data.Replace("\n", "\n    "))
                                                                                 );
    }
}