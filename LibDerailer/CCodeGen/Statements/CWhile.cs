using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibDerailer.CCodeGen.Statements.Expressions;

namespace LibDerailer.CCodeGen.Statements
{
    public class CWhile : CStatement
    {
        public CExpression Predicate { get; set; }
        public CBlock      Body      { get; set; }

        public CWhile(CExpression predicate, CBlock body = null)
        {
            Predicate = predicate;
            Body      = body ?? new CBlock();
        }

        public override string ToString()
        {
            if (Body == null || Body.Statements.Count == 0)
                return $"while ({Predicate});";

            return $"while ({Predicate})\n" +
                   "{" +
                   $"{AstUtil.Indent(Body.ToString())}\n" +
                   "}";
        }

        public override IEnumerable<CToken> ToTokens()
        {
            yield return new CToken(CTokenType.Keyword, "while");
            yield return new CToken(CTokenType.Whitespace, " ");
            yield return new CToken(CTokenType.OpenParen, "(");
            foreach (var tok in Predicate.ToTokens())
                yield return tok;
            yield return new CToken(CTokenType.CloseParen, ")");
            yield return new CToken(CTokenType.Whitespace, "\n");

            foreach (var tok in AstUtil.Indent(Body.ToTokens()))
                yield return tok;
        }
    }
}