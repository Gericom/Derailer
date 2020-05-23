namespace LibDerailer.CCodeGen.Statements.Expressions
{
    public class CVariable : CLiteral
    {
        public string Name { get; set; }

        public CVariable(string name)
        {
            Name = name;
        }

        public override string ToString() => Name;

        public CMethodCall this[CExpression idx]
            => new CMethodCall(true, "[]", this, idx);
    }
}