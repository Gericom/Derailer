using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibDerailer.CCodeGen
{
    public class Method
    {
        public bool IsStatic { get; set; }
        public string   Name       { get; set; }
        public TypeName ReturnType { get; set; } = new TypeName();

        public List<(TypeName type, string name)> Parameters { get; } = new List<(TypeName type, string name)>();

        public Block Body { get; set; } = new Block();

        public Method(string name, params (TypeName type, string name)[] parameters)
        {
            Name = name;
            Parameters.AddRange(parameters);
        }

        public Method(TypeName returnType, string name, params (TypeName type, string name)[] parameters)
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