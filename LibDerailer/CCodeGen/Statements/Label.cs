namespace LibDerailer.CCodeGen.Statements
{
    public class Label : Statement
    {
        public string Name { get; set; }

        public Label(string name)
        {
            Name = name;
        }

        public override string ToString() => $"{Name}:;";
    }
}