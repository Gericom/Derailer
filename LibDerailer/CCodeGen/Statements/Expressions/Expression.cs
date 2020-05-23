namespace LibDerailer.CCodeGen.Statements.Expressions
{
    public abstract class Expression : Statement
    {
        public static implicit operator Expression(bool a)
            => new RawLiteral<bool>(a);

        public static implicit operator Expression(sbyte a)
            => new RawLiteral<sbyte>(a);

        public static implicit operator Expression(byte a)
            => new RawLiteral<byte>(a);

        public static implicit operator Expression(short a)
            => new RawLiteral<short>(a);

        public static implicit operator Expression(ushort a)
            => new RawLiteral<ushort>(a);

        public static implicit operator Expression(int a)
            => new RawLiteral<int>(a);

        public static implicit operator Expression(uint a)
            => new RawLiteral<uint>(a);

        public static implicit operator Expression(float a)
            => new RawLiteral<float>(a);

        public static implicit operator Expression(string a)
            => new StringLiteral(a);

        public static MethodCall operator ==(Expression a, Expression b)
            => new MethodCall(true, "==", a, b);

        public static MethodCall operator !=(Expression a, Expression b)
            => new MethodCall(true, "!=", a, b);

        public static MethodCall operator <(Expression a, Expression b)
            => new MethodCall(true, "<", a, b);

        public static MethodCall operator >(Expression a, Expression b)
            => new MethodCall(true, ">", a, b);

        public static MethodCall operator <=(Expression a, Expression b)
            => new MethodCall(true, "<=", a, b);

        public static MethodCall operator >=(Expression a, Expression b)
            => new MethodCall(true, ">=", a, b);

        public static MethodCall operator +(Expression a, Expression b)
            => new MethodCall(true, "+", a, b);

        public static MethodCall operator -(Expression a, Expression b)
            => new MethodCall(true, "-", a, b);

        public static MethodCall operator *(Expression a, Expression b)
            => new MethodCall(true, "*", a, b);

        public static MethodCall operator /(Expression a, Expression b)
            => new MethodCall(true, "/", a, b);

        public static MethodCall operator %(Expression a, Expression b)
            => new MethodCall(true, "%", a, b);

        public static MethodCall operator &(Expression a, Expression b)
            => new MethodCall(true, "&", a, b);

        public static MethodCall operator |(Expression a, Expression b)
            => new MethodCall(true, "|", a, b);

        public static MethodCall operator <<(Expression a, int b)
            => new MethodCall(true, "<<", a, b);

        public static MethodCall operator >>(Expression a, int b)
            => new MethodCall(true, ">>", a, b);

        public static MethodCall operator -(Expression a)
            => new MethodCall(true, "-", a);

        public static MethodCall operator +(Expression a)
            => new MethodCall(true, "+", a);

        public static MethodCall operator !(Expression a)
            => new MethodCall(true, "!", a);

        public static MethodCall operator ~(Expression a)
            => new MethodCall(true, "~", a);

        public static MethodCall Assign(Expression a, Expression b)
            => new MethodCall(true, "=", a, b);
    }
}