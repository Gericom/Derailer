using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibDerailer.CCodeGen.Statements.Expressions;

namespace LibDerailer.CCodeGen.Statements
{
    public class CDoWhile : CStatement
    {
        public CExpression Predicate { get; set; }
        public CBlock      Body      { get; set; }

        public CDoWhile(CExpression predicate, CBlock body = null)
        {
            Predicate = predicate;
            Body      = body ?? new CBlock();
        }

        public override string ToString() 
            => "do\n" +
               "{" +
               $"{AstUtil.Indent(Body.ToString())}\n" +
               "}\n" +
               $"while ({Predicate});";

        public override IEnumerable<CToken> ToTokens()
        {
            yield return new CToken(CTokenType.Keyword, "do");
            yield return new CToken(CTokenType.Whitespace, "\n");

            foreach (var tok in Body.ToTokens())
                yield return tok;
            yield return new CToken(CTokenType.Keyword, "while");
            yield return new CToken(CTokenType.Whitespace, " ");
            yield return new CToken(CTokenType.OpenParen, "(");
            foreach (var tok in Predicate.ToTokens())
                yield return tok;
            yield return new CToken(CTokenType.CloseParen, ")");
            yield return new CToken(CTokenType.Semicolon, ";");
        }
    }
}