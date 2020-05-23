namespace LibDerailer.CCodeGen.Statements.Expressions
{
    public class Cast : Expression
    {
        public TypeName   Type       { get; set; }
        public Expression Expression { get; set; }

        public Cast(TypeName type, Expression expression)
        {
            Type       = type;
            Expression = expression;
        }

        public override string ToString()
        {
            string expr = Expression.ToString();
            if (!(Expression is Literal) && (Expression as MethodCall)?.Name != "[]")
                Expression = $"({Expression})";
            return $"({Type}) {expr}";
        }
    }
}