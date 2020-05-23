namespace LibDerailer.CCodeGen.Statements
{
    public class Goto : Statement
    {
        public string Label { get; set; }

        public Goto(string label)
        {
            Label = label;
        }

        public override string ToString() 
            => $"goto {Label};";
    }
}