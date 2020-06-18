using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibDerailer.CCodeGen
{
    public class CMethod : IASTNode
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

        public override IEnumerable<CToken> ToTokens()
        {
            if (IsStatic)
            {
                yield return new CToken(CTokenType.Keyword, "static");
                yield return new CToken(CTokenType.Whitespace, " ");
            }
            yield return new CToken(CTokenType.Type, ReturnType.ToString());
            yield return new CToken(CTokenType.Whitespace, " ");
            yield return new CToken(CTokenType.Identifier, Name);
            yield return new CToken(CTokenType.OpenParen, "(");


            for (int i = 0; i < Parameters.Count; ++i) {
                var param = Parameters[i];

                yield return new CToken(CTokenType.Type, param.type.ToString());
                yield return new CToken(CTokenType.Whitespace, " ");
                yield return new CToken(CTokenType.Identifier, param.name);

                if (i + 1 != Parameters.Count)
                {
                    yield return new CToken(CTokenType.Comma, ",");
                    yield return new CToken(CTokenType.Whitespace, " ");
                }
            }
            
            yield return new CToken(CTokenType.CloseParen, ")");
            yield return new CToken(CTokenType.Whitespace, "\n");
            yield return new CToken(CTokenType.OpenBrace, "{");
            yield return new CToken(CTokenType.Whitespace, "\n");

            foreach (var tok in Body.ToTokens(true))
                yield return tok;

            yield return new CToken(CTokenType.CloseBrace, "}");
            yield return new CToken(CTokenType.Whitespace, "\n");
        }
    }
}