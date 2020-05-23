namespace LibDerailer.CCodeGen.Statements.Expressions
{
    public class CStringLiteral : CLiteral
    {
        public string Value { get; set; }

        public CStringLiteral(string v)
        {
            Value = v;
        }

        public override string ToString() => $"\"{Value}\"";
    };
}