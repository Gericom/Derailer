using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibDerailer.CCodeGen.Statements.Expressions;

namespace LibDerailer.CCodeGen.Statements
{
    public class CFor : CStatement
    {
        public CStatement Initial { get; set; }
        public CStatement Update { get; set; }
        public CExpression Predicate { get; set; }
        public CBlock Body { get; set; } = new CBlock();

        public CFor()
        {
        }

        public CFor(CStatement initial, CStatement update, CExpression predicate, CBlock body)
        {
            Initial = initial;
            Update = update;
            Predicate = predicate;
            Body = body;
        }

        public override string ToString()
            => $"for ({Initial}; {Predicate}; {Update})\n" +
               "{" +
               $"{AstUtil.Indent(Body.ToString())}\n" +
               "}";

        public override IEnumerable<CToken> ToTokens()
        {
            yield return new CToken(CTokenType.Keyword, "for");
            yield return new CToken(CTokenType.Whitespace, " ");
            yield return new CToken(CTokenType.OpenParen, "(");
            
            if (!(Initial is null))
                foreach (var tok in Initial.ToTokens())
                    yield return tok;
            yield return new CToken(CTokenType.Semicolon, ";");
            yield return new CToken(CTokenType.Whitespace, " ");

            if (!(Predicate is null))
                foreach (var tok in Predicate.ToTokens())
                    yield return tok;
            yield return new CToken(CTokenType.Semicolon, ";");
            yield return new CToken(CTokenType.Whitespace, " ");

            if (!(Update is null))
                foreach (var tok in Update.ToTokens())
                    yield return tok;
            yield return new CToken(CTokenType.CloseParen, ")");
            yield return new CToken(CTokenType.Whitespace, "\n");

            foreach (var tok in AstUtil.Indent(Body.ToTokens()))
                yield return tok;
        }
    }
}