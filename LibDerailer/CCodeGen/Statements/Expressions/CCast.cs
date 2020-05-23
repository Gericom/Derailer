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
    }
}