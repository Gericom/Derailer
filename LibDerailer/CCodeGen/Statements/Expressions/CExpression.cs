namespace LibDerailer.CCodeGen.Statements.Expressions
{
    public abstract class CExpression : CStatement
    {
        public static implicit operator CExpression(bool a)
            => new CRawLiteral<bool>(a);

        public static implicit operator CExpression(sbyte a)
            => new CRawLiteral<sbyte>(a);

        public static implicit operator CExpression(byte a)
            => new CRawLiteral<byte>(a);

        public static implicit operator CExpression(short a)
            => new CRawLiteral<short>(a);

        public static implicit operator CExpression(ushort a)
            => new CRawLiteral<ushort>(a);

        public static implicit operator CExpression(int a)
            => new CRawLiteral<int>(a);

        public static implicit operator CExpression(uint a)
            => new CRawLiteral<uint>(a);

        public static implicit operator CExpression(float a)
            => new CRawLiteral<float>(a);

        public static implicit operator CExpression(string a)
            => new CStringLiteral(a);

        public static CMethodCall operator ==(CExpression a, CExpression b)
            => new CMethodCall(true, "==", a, b);

        public static CMethodCall operator !=(CExpression a, CExpression b)
            => new CMethodCall(true, "!=", a, b);

        public static CMethodCall operator <(CExpression a, CExpression b)
            => new CMethodCall(true, "<", a, b);

        public static CMethodCall operator >(CExpression a, CExpression b)
            => new CMethodCall(true, ">", a, b);

        public static CMethodCall operator <=(CExpression a, CExpression b)
            => new CMethodCall(true, "<=", a, b);

        public static CMethodCall operator >=(CExpression a, CExpression b)
            => new CMethodCall(true, ">=", a, b);

        public static CMethodCall operator +(CExpression a, CExpression b)
            => new CMethodCall(true, "+", a, b);

        public static CMethodCall operator -(CExpression a, CExpression b)
            => new CMethodCall(true, "-", a, b);

        public static CMethodCall operator *(CExpression a, CExpression b)
            => new CMethodCall(true, "*", a, b);

        public static CMethodCall operator /(CExpression a, CExpression b)
            => new CMethodCall(true, "/", a, b);

        public static CMethodCall operator %(CExpression a, CExpression b)
            => new CMethodCall(true, "%", a, b);

        public static CMethodCall operator &(CExpression a, CExpression b)
            => new CMethodCall(true, "&", a, b);

        public static CMethodCall operator |(CExpression a, CExpression b)
            => new CMethodCall(true, "|", a, b);

        public static CMethodCall operator ^(CExpression a, CExpression b)
            => new CMethodCall(true, "^", a, b);

        public static CMethodCall operator <<(CExpression a, int b)
            => new CMethodCall(true, "<<", a, b);

        public static CMethodCall operator >>(CExpression a, int b)
            => new CMethodCall(true, ">>", a, b);

        public static CMethodCall operator -(CExpression a)
            => new CMethodCall(true, "-", a);

        public static CMethodCall operator +(CExpression a)
            => new CMethodCall(true, "+", a);

        public static CMethodCall operator !(CExpression a)
            => new CMethodCall(true, "!", a);

        public static CMethodCall operator ~(CExpression a)
            => new CMethodCall(true, "~", a);

        public static CMethodCall Assign(CExpression a, CExpression b)
            => new CMethodCall(true, "=", a, b);

        public static CMethodCall Deref(CExpression a)
            => new CMethodCall(true, "*", a);

        public static CMethodCall Ref(CExpression a)
            => new CMethodCall(true, "&", a);
    }
}