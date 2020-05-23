using System;
using System.Globalization;

namespace LibDerailer.CCodeGen.Statements.Expressions
{
    public class RawLiteral<T> : Literal where T : IConvertible
    {
        public T Value { get; set; }

        public RawLiteral(T v)
        {
            Value = v;
        }

        public override string ToString()
        {
            if(Value is bool b)
                return b ? "TRUE" : "FALSE";
            return Value.ToString(CultureInfo.InvariantCulture);
        }

        public static implicit operator RawLiteral<T>(T a)
        {
            return new RawLiteral<T>(a);
        }
    }
}