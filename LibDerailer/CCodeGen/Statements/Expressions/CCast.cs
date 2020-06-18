using System.Collections.Generic;
using System.Linq;

namespace LibDerailer.CCodeGen.Statements.Expressions
{
    public class CCast : CExpression
    {
        public CType       Type       { get; set; }
        public CExpression Expression { get; set; }

        public CCast(CType type, CExpression expression)
        {
            Type       = type;
            Expression = expression;
        }

        public override string ToString()
        {
            string expr = Expression.ToString();
            if (!(Expression is CLiteral) && (Expression as CMethodCall)?.Name != "[]")
                expr = $"({expr})";
            return $"({Type}) {expr}";
        }

        public override IEnumerable<CToken> ToTokens()
        {
            yield return new CToken(CTokenType.Cast, $"({Type})");
            yield return new CToken(CTokenType.Whitespace, " ");

            foreach (var tok in Expression.ToTokens())
                yield return tok;
        }
    }
}