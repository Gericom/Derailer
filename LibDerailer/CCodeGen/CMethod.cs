using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibDerailer.CCodeGen
{
    public class CMethod
    {
        public bool IsStatic { get; set; }
        public string   Name       { get; set; }
        public CType ReturnType { get; set; } = new CType();

        public List<(CType type, string name)> Parameters { get; } = new List<(CType type, string name)>();

        public CBlock Body { get; set; } = new CBlock();

        public CMethod(string name, params (CType type, string name)[] parameters)
        {
            Name = name;
            Parameters.AddRange(parameters);
        }

        public CMethod(CType returnType, string name, params (CType type, string name)[] parameters)
        {
            ReturnType = returnType;
            Name       = name;
            Parameters.AddRange(parameters);
        }

        public override string ToString()
        {
            string paramString = string.Join(", ", Parameters.Select(param => $"{param.type} {param.name}"));
            return $"{(IsStatic ? "static " : "")}{ReturnType} {Name}({paramString})\n" +
                   "{" +
                   $"{AstUtil.Indent(Body.ToString())}\n" +
                   "}\n";
        }
    }
}