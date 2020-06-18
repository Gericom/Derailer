using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibDerailer.CCodeGen.Statements.Expressions;

namespace LibDerailer.CCodeGen.Statements
{
    public class CIf : CStatement
    {
        public CExpression Predicate { get; set; }
        public CBlock      IfBody    { get; set; }
        public CBlock      ElseBody  { get; set; }

        public CIf(CExpression predicate, CBlock ifBody = null, CBlock elseBody = null)
        {
            Predicate = predicate;
            IfBody    = ifBody ?? new CBlock();
            ElseBody  = elseBody;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.Append($"if ({Predicate})\n{{");
            builder.Append(AstUtil.Indent(IfBody.ToString()));

            if (ElseBody != null)
            {
                builder.Append("\n}\nelse\n{");
                builder.Append(AstUtil.Indent(ElseBody.ToString()));
            }

            builder.Append("\n}");

            return builder.ToString();
        }

        public override IEnumerable<CToken> ToTokens()
        {
            yield return new CToken(CTokenType.Keyword, "if");
            yield return new CToken(CTokenType.Whitespace, " ");
            yield return new CToken(CTokenType.OpenParen, "(");
            foreach (var tok in Predicate.ToTokens())
                yield return tok;
            yield return new CToken(CTokenType.CloseParen, ")");
            yield return new CToken(CTokenType.Whitespace, "\n");

            foreach (var tok in IfBody.ToTokens())
                yield return tok;
 
            if (!(ElseBody is null))
            {
                yield return new CToken(CTokenType.Keyword, "else");
                yield return new CToken(CTokenType.Whitespace, "\n");

                foreach (var tok in ElseBody.ToTokens())
                    yield return tok;
            }
        }
    }
}