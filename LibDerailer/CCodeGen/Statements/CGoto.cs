namespace LibDerailer.CCodeGen.Statements
{
    public class CGoto : CStatement
    {
        public string Label { get; set; }

        public CGoto(string label)
        {
            Label = label;
        }

        public override string ToString() 
            => $"goto {Label};";
    }
}