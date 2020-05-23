namespace LibDerailer.CCodeGen.Statements
{
    public class CLabel : CStatement
    {
        public string Name { get; set; }

        public CLabel(string name)
        {
            Name = name;
        }

        public override string ToString() => $"{Name}:;";
    }
}