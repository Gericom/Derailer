using System;
using System.Collections.Generic;
using System.Linq;

namespace LibDerailer.CCodeGen.Statements.Expressions
{
    public class CMethodCall : CExpression
    {
        public string Name { get; set; }
        public bool IsOperator { get; set; }
        public List<CExpression> Arguments { get; } = new List<CExpression>();

        public CMethodCall(bool isOperator, string name, params CExpression[] arguments)
        {
            IsOperator = isOperator;
            Name = name;
            Arguments.AddRange(arguments);
        }

        public override string ToString()
        {
            if (!IsOperator)
                return $"{Name}({string.Join(",", Arguments)})";

            if (Arguments.Count > 2)
                throw new InvalidOperationException("Only binary or unary operators supported!");
            if (Name == "[]")
                return $"{Arguments[0]}[{Arguments[1]}]";

            if (Arguments.Count == 1)
                return Arguments[0] is CLiteral ||
                       (Arguments[0] as CMethodCall)?.Name == "[]" ||
                       (Arguments[0] as CMethodCall)?.Name == "->" ||
                       (Arguments[0] as CMethodCall)?.Name == "." ||
                       (Arguments[0] as CMethodCall)?.IsOperator == false
                    ? $"{Name}{Arguments[0]}"
                    : $"{Name}({Arguments[0]})";

            string arg0 = Arguments[0].ToString();
            if (Name != "=" && !(Arguments[0] is CLiteral) &&
                (Arguments[0] as CMethodCall)?.Name != "[]" &&
                (Arguments[0] as CMethodCall)?.Name != "->" &&
                (Arguments[0] as CMethodCall)?.Name != "." &&
                (Arguments[0] as CMethodCall)?.IsOperator == true)
                arg0 = $"({arg0})";

            string arg1 = Arguments[1].ToString();
            if (Name != "=" && !(Arguments[1] is CLiteral) &&
                (Arguments[1] as CMethodCall)?.Name != "[]" &&
                (Arguments[1] as CMethodCall)?.Name != "->" &&
                (Arguments[1] as CMethodCall)?.Name != "." &&
                (Arguments[1] as CMethodCall)?.IsOperator == true)
                arg1 = $"({arg1})";

            if (Name == "=" &&
                Arguments[0] is CVariable arg0var &&
                Arguments[1] is CMethodCall a1mc &&
                a1mc.IsOperator &&
                a1mc.Name == "+" &&
                a1mc.Arguments.Count == 2 &&
                a1mc.Arguments[0] is CVariable arg1var &&
                arg0var.Name == arg1var.Name &&
                (a1mc.Arguments[1].ToString() == "1" || a1mc.Arguments[1].ToString() == "0x1"))
                return $"{arg0}++";

            if (Name == "." || Name == "->")
                return $"{arg0}{Name}{arg1}";

            return $"{arg0} {Name} {arg1}";
        }

        public override IEnumerable<CToken> ToTokens()
        {
            if (!IsOperator)
            {
                yield return new CToken(CTokenType.Identifier, Name);
                yield return new CToken(CTokenType.OpenParen, "(");

                for (int i = 0; i < Arguments.Count; ++i)
                {
                    var arg = Arguments[i];

                    foreach (var tok in arg.ToTokens())
                        yield return tok;

                    if (i + 1 != Arguments.Count)
                    {
                        yield return new CToken(CTokenType.Comma, ",");
                        yield return new CToken(CTokenType.Whitespace, " ");
                    }
                }

                yield return new CToken(CTokenType.CloseParen, ")");
                yield break;
            }

            if (Arguments.Count > 2)
                throw new InvalidOperationException("Only binary or unary operators supported!");
            if (Name == "[]")
            {
                foreach (var tok in Arguments[0].ToTokens())
                    yield return tok;
                yield return new CToken(CTokenType.OpenBracket, "[");
                foreach (var tok in Arguments[1].ToTokens())
                    yield return tok;
                yield return new CToken(CTokenType.CloseBracket, "]");

                yield break;
            }
            if (Arguments.Count == 1)
            {
                yield return new CToken(CTokenType.Identifier, Name);
                if (Arguments[0] is CLiteral ||
                   (Arguments[0] as CMethodCall)?.Name == "[]" ||
                   (Arguments[0] as CMethodCall)?.Name == "->" ||
                   (Arguments[0] as CMethodCall)?.Name == "." ||
                   (Arguments[0] as CMethodCall)?.IsOperator == false)
                {
                    foreach (var tok in Arguments[0].ToTokens())
                        yield return tok;

                    yield break;
                }

                yield return new CToken(CTokenType.OpenParen, "(");
                foreach (var tok in Arguments[0].ToTokens())
                    yield return tok;
                yield return new CToken(CTokenType.CloseParen, ")");

                yield break;
            }

            var arg0 = Arguments[0].ToTokens().ToList();
            if (Name != "=" && !(Arguments[0] is CLiteral) &&
                (Arguments[0] as CMethodCall)?.Name != "[]" &&
                (Arguments[0] as CMethodCall)?.Name != "->" &&
                (Arguments[0] as CMethodCall)?.Name != "." &&
                (Arguments[0] as CMethodCall)?.IsOperator == true)
            {
                arg0.Prepend(new CToken(CTokenType.OpenParen, "("));
                arg0.Add(new CToken(CTokenType.CloseParen, ")"));
            }
            var arg1 = Arguments[1].ToTokens().ToList();
            if (Name != "=" && !(Arguments[1] is CLiteral) &&
                (Arguments[1] as CMethodCall)?.Name != "[]" &&
                (Arguments[1] as CMethodCall)?.Name != "->" &&
                (Arguments[1] as CMethodCall)?.Name != "." &&
                (Arguments[1] as CMethodCall)?.IsOperator == true)
            {
                arg1.Prepend(new CToken(CTokenType.OpenParen, "("));
                arg1.Add(new CToken(CTokenType.CloseParen, ")"));
            }

            if (Name == "=" &&
                Arguments[0] is CVariable arg0var &&
                Arguments[1] is CMethodCall a1mc &&
                a1mc.IsOperator &&
                a1mc.Name == "+" &&
                a1mc.Arguments.Count == 2 &&
                a1mc.Arguments[0] is CVariable arg1var &&
                arg0var.Name == arg1var.Name &&
                (a1mc.Arguments[1].ToString() == "1" || a1mc.Arguments[1].ToString() == "0x1"))
            {
                arg0.Add(new CToken(CTokenType.Operator, "++"));
                foreach (var arg in arg0)
                    yield return arg;
                yield break;
            }

            if (Name != "." && Name != "->")
                arg0.Add(new CToken(CTokenType.Whitespace, " "));
            arg0.Add(new CToken(CTokenType.Operator, Name));
            if (Name != "." && Name != "->")
                arg0.Add(new CToken(CTokenType.Whitespace, " "));
            arg0 = arg0.Concat(arg1).ToList();
            
            foreach (var arg in arg0)
                yield return arg;
        }
    }
}