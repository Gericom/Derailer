namespace LibDerailer.CCodeGen
{
    public static class AstUtil
    {
        public static string Indent(string s)
        {
            return s.Replace("\n", "\n    ");
        }
    }
}