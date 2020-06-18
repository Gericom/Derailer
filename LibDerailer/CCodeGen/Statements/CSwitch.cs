using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.CCodeGen.Statements.Expressions;

namespace LibDerailer.CCodeGen.Statements
{
    public class CSwitch : CStatement
    {
        public CExpression Expression { get; set; }

        public List<(CLiteral caseVal, CBlock caseBlock)> Cases { get; } =
            new List<(CLiteral caseVal, CBlock caseBlock)>();

        public CSwitch(CExpression expression, params (CLiteral caseVal, CBlock caseBlock)[] cases)
        {
            Expression = expression;
            Cases.AddRange(cases);
        }

        public override string ToString()
        {
            string result = $"switch ({Expression})\n{{\n";
            foreach (var (val, block) in Cases)
            {
                if (val is null)
                    result += "    default:";
                else
                    result += $"    case {val}:";

                if (block.Statements.Count == 0)
                    result += " break;\n";
                else
                    result += $"\n    {{{AstUtil.Indent(AstUtil.Indent(block.ToString()))}\n        break;\n    }}\n";
            }

            return result + "}";
        }

        public override IEnumerable<CToken> ToTokens()
        {
            yield return new CToken(CTokenType.Keyword, "switch");
            yield return new CToken(CTokenType.Whitespace, " ");
            yield return new CToken(CTokenType.OpenParen, "(");
            foreach (var tok in Expression.ToTokens())
                yield return tok;
            yield return new CToken(CTokenType.CloseParen, ")");
            yield return new CToken(CTokenType.Whitespace, "\n");
            yield return new CToken(CTokenType.OpenBrace, "{");
            yield return new CToken(CTokenType.Whitespace, "\n");

            foreach (var (val, block) in Cases)
            {
                if (val is null)
                    yield return new CToken(CTokenType.Keyword, "default");
                else
                {
                    yield return new CToken(CTokenType.Keyword, "case");
                    yield return new CToken(CTokenType.Whitespace, " ");
                    foreach (var tok in val.ToTokens())
                        yield return tok;
                }
                yield return new CToken(CTokenType.Colon, ":");
                yield return new CToken(CTokenType.Whitespace, "\n");

                // TODO: Support fallthrough
                // TODO: Assumes no declarations inside the block!

                foreach (var statement in block.Statements)
                {
                    yield return new CToken(CTokenType.Whitespace, "    ");
                    foreach (var tok in AstUtil.Indent(statement.ToTokens()))
                        yield return tok;
                    if (statement is CExpression)
                        yield return new CToken(CTokenType.Semicolon, ";");
                    yield return new CToken(CTokenType.Whitespace, "\n");
                }

                yield return new CToken(CTokenType.Whitespace, "    ");
                yield return new CToken(CTokenType.Keyword, "break");
                yield return new CToken(CTokenType.Semicolon, ";");
                yield return new CToken(CTokenType.Whitespace, "\n");
            }

            yield return new CToken(CTokenType.CloseBrace, "}");
        }
    }
}