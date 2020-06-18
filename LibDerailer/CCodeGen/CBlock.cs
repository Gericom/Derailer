using System.Collections.Generic;
using System.Linq;
using LibDerailer.CCodeGen.Statements;
using LibDerailer.CCodeGen.Statements.Expressions;

namespace LibDerailer.CCodeGen
{
    public class CBlock : IASTNode
    {
        public List<CStatement> Statements { get; } = new List<CStatement>();

        public CBlock(params CStatement[] statements)
        {
            Statements.AddRange(statements);
        }

        public override string ToString()
        {
            return "\n" + string.Join("\n",
                from st in Statements select (st is CExpression ? st.ToString() + ";" : st.ToString()));
        }

        public IEnumerable<CToken> ToTokens(bool neverBlocks)
        {
            if (Statements.Count == 0)
            {
                yield return new CToken(CTokenType.Whitespace, "    ");
                yield return new CToken(CTokenType.Semicolon, ";");
                yield return new CToken(CTokenType.Whitespace, "\n");
                yield break;
            }

            if (Statements.Count == 1)
            {
                yield return new CToken(CTokenType.Whitespace, "    ");
                foreach (var tok in AstUtil.Indent(Statements[0].ToTokens()))
                    yield return tok;
                if (Statements[0] is CExpression)
                    yield return new CToken(CTokenType.Semicolon, ";");

                yield return new CToken(CTokenType.Whitespace, "\n");
                yield break;
            }

            if (!neverBlocks)
            {
                yield return new CToken(CTokenType.OpenBrace, "{");
                yield return new CToken(CTokenType.Whitespace, "\n");
            }

            foreach (var statement in Statements)
            {
                yield return new CToken(CTokenType.Whitespace, "    ");
                foreach (var tok in AstUtil.Indent(statement.ToTokens()))
                    yield return tok;
                if (statement is CExpression)
                    yield return new CToken(CTokenType.Semicolon, ";");

                yield return new CToken(CTokenType.Whitespace, "\n");
            }

            if (!neverBlocks)
            {
                yield return new CToken(CTokenType.CloseBrace, "}");
                yield return new CToken(CTokenType.Whitespace, "\n");
            }
        }

        public override IEnumerable<CToken> ToTokens() => ToTokens(false);
    }
}