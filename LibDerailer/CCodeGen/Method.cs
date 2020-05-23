using System.Collections.Generic;
using System.Text;

namespace LibDerailer.CCodeGen
{
    public class Method
    {
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
            var builder      = new StringBuilder();
            var param_string = "";
            foreach (var param in Parameters)
                param_string += $"{param.Item1} {param.Item2}";
            builder.Append($"{ReturnType} {Name}({param_string})\n{{");

            builder.Append(AstUtil.Indent(Body.ToString()));

            builder.Append("\n}\n");
            return builder.ToString();
        }
    }
}