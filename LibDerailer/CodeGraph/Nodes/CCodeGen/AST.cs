using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDerailer.CodeGraph.Nodes.CCodeGen
{
    public static class Util
    {
        public static string Indent(string s)
        {
            return s.Replace("\n", "\n    ");
        }
    }


    public class Statement { }
    public class Expression : Statement { }
    public class RawLiteral<T> : Expression
    {
        public T value;
        public RawLiteral(T v)
        {
            value = v;
        }

        public override string ToString()
        {
            return value.ToString();
        }
    }
    public class StringLiteral : Expression
    {
        public string value;
        public StringLiteral(string v)
        {
            value = v;
        }

        public override string ToString()
        {
            return $"\"{value}\"";
        }
    };
    public class ProcedureCall : Expression
    {
        public string signature;
        public bool is_operator = false;
        public List<Expression> arguments;

        public override string ToString()
        {
            if (!is_operator)
                return $"{signature}({string.Join(",", arguments)})";

            if (arguments.Count > 2) throw new InvalidOperationException("Only binary or unary operators supported!");
            if (signature == "[]") return $"{arguments[0]}[{arguments[1]}]";
            return arguments.Count == 1 ? $"{signature}{arguments[0]}" : $"{arguments[0]} {signature} {arguments[1]}";
        }
    }
    public class TypeName
    {
        public bool is_pointer = false;

        /* Not sure how to do this best in C#.
         * In C++ I'd use a std::optional<std::string, std::unique_ptr<TypeName>> or something.
         */
        public string name = null;
        public TypeName typename = null;

        public override string ToString()
        {
            if (name == null && typename == null)
                return is_pointer ? "void*" : "void";
            if (name != null && typename != null)
                throw new InvalidOperationException("Only one option may be set");

            var enclosed = name != null ? name : typename.ToString();
            if (is_pointer)
                enclosed += "*";
            return enclosed;
        }
    }
    public class Cast : Expression
    {
        public TypeName typename;
        public Expression expression;

        public override string ToString()
        {
            return $"({typename}) {expression}";
        }
    }
    public class Block
    {
        public List<Statement> statements = new List<Statement>();
        public override string ToString()
        {
            return "\n" + string.Join("\n",
                from st in statements select (st is Expression ? st.ToString() + ";" : st.ToString()));
        }
    }
    public class IfStatement : Statement
    {
        public Expression predicate = null;
        public Block if_body = new Block();
        public Block else_body = null;

        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.Append($"if ({ predicate }) {{");
            builder.Append(Util.Indent(if_body.ToString()));

            if (else_body != null)
            {
                builder.Append("} else {");
                builder.Append(Util.Indent(else_body.ToString()));
            }

            builder.Append("\n}");

            return builder.ToString();
        }
    }
    public class WhileStatement : Statement
    {
        public Expression predicate = null;
        public Block body = new Block();

        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.Append($"while ({ predicate }) {{");
            builder.Append(Util.Indent(body.ToString()));
            builder.Append("\n}");

            return builder.ToString();
        }
    }
    public class ForStatement : Statement
    {
        public Statement initial = null;
        public Statement update = null;
        public Expression predicate = null;
        public Block body = new Block();

        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.Append($"for ({ initial }; { predicate }; { update }) {{");
            builder.Append(Util.Indent(body.ToString()));
            builder.Append("\n}");

            return builder.ToString();
        }
    }
    public class GotoStatement : Statement
    {
        public string label;

        public override string ToString()
        {
            return $"goto {label};";
        }
    }
    public class LabelStatement : Statement
    {
        public string label;

        public override string ToString()
        {
            return $"{label}:;";
        }
    }
    public class Procedure
    {
        public string signature;
        public TypeName return_type = new TypeName();
        public List<Tuple<TypeName, string>> parameters = new List<Tuple<TypeName, string>>();
        public Block body = new Block();

        public override string ToString()
        {
            var builder = new StringBuilder();
            var param_string = "";
            foreach (var param in parameters)
                param_string += $"{param.Item1} {param.Item2}";
            builder.Append($"{return_type} {signature}({param_string}) {{");

            builder.Append(Util.Indent(body.ToString()));

            builder.Append("\n}\n");
            return builder.ToString();
        }
    }
}
