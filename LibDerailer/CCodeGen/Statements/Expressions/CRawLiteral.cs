using System;
using System.Globalization;

namespace LibDerailer.CCodeGen.Statements.Expressions
{
    public class CRawLiteral<T> : CLiteral where T : IConvertible
    {
        public T Value { get; set; }

        public CRawLiteral(T v)
        {
            Value = v;
        }

        public override string ToString()
        {
            if (Value is bool b)
                return b ? "TRUE" : "FALSE";
            if (Value is uint v)
                return $"0x{Value:X}";
            return Value.ToString(CultureInfo.InvariantCulture);
        }

        public static implicit operator CRawLiteral<T>(T a)
        {
            return new CRawLiteral<T>(a);
        }
    }
}