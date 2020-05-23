namespace LibDerailer.CCodeGen.Statements.Expressions
{
    public class StringLiteral : Literal
    {
        public string Value { get; set; }

        public StringLiteral(string v)
        {
            Value = v;
        }

        public override string ToString() => $"\"{Value}\"";
    };
}