namespace LibDerailer.CCodeGen.Statements.Expressions
{
    public class Variable : Literal
    {
        public string Name { get; set; }

        public Variable(string name)
        {
            Name = name;
        }

        public override string ToString() => Name;

        public MethodCall this[Expression idx]
            => new MethodCall(true, "[]", this, idx);
    }
}