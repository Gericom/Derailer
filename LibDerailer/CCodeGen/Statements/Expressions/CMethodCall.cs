using System;
using System.Collections.Generic;

namespace LibDerailer.CCodeGen.Statements.Expressions
{
    public class CMethodCall : CExpression
    {
        public string            Name       { get; set; }
        public bool              IsOperator { get; set; }
        public List<CExpression> Arguments  { get; } = new List<CExpression>();

        public CMethodCall(bool isOperator, string name, params CExpression[] arguments)
        {
            IsOperator = isOperator;
            Name       = name;
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
                return Arguments[0] is CLiteral || (Arguments[0] as CMethodCall)?.Name == "[]" ||
                       (Arguments[0] as CMethodCall)?.IsOperator == false
                    ? $"{Name}{Arguments[0]}"
                    : $"{Name}({Arguments[0]})";

            string arg0 = Arguments[0].ToString();
            if (Name != "=" && !(Arguments[0] is CLiteral) && (Arguments[0] as CMethodCall)?.Name != "[]" &&
                (Arguments[0] as CMethodCall)?.IsOperator == true)
                arg0 = $"({arg0})";

            string arg1 = Arguments[1].ToString();
            if (Name != "=" && !(Arguments[1] is CLiteral) && (Arguments[1] as CMethodCall)?.Name != "[]" &&
                (Arguments[1] as CMethodCall)?.IsOperator == true)
                arg1 = $"({arg1})";

            return $"{arg0} {Name} {arg1}";
        }
    }
}