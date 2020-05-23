using System;
using System.Collections.Generic;

namespace LibDerailer.CCodeGen.Statements.Expressions
{
    public class MethodCall : Expression
    {
        public string           Name       { get; set; }
        public bool             IsOperator { get; set; }
        public List<Expression> Arguments  { get; } = new List<Expression>();

        public MethodCall(bool isOperator, string name, params Expression[] arguments)
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

            if(Arguments.Count == 1)
                return Arguments[0] is Literal || (Arguments[0] as MethodCall)?.Name == "[]" ? $"{Name}{Arguments[0]}" : $"{Name}({Arguments[0]})";

            string arg0 = Arguments[0].ToString();
            if (!(Arguments[0] is Literal) && (Arguments[0] as MethodCall)?.Name != "[]")
                arg0 = $"({arg0})";

            string arg1 = Arguments[1].ToString();
            if (!(Arguments[1] is Literal) && (Arguments[1] as MethodCall)?.Name != "[]")
                arg1 = $"({arg1})";

            return $"{arg0} {Name} {arg1}";
        }
    }
}