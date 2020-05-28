using System;
using LibDerailer.CCodeGen.Statements.Expressions;

namespace LibDerailer.IR.Expressions
{
    public abstract class IRConstant : IRExpression
    {
        protected IRConstant(IRType type) 
            : base(type)
        {
        }
    }

    public class IRConstant<T> : IRConstant where T : struct, IConvertible
    {
        private static IRType GetIRType(T value)
        {
            switch (value)
            {
                case bool _:
                    return IRType.I1;
                case sbyte _:
                case byte _:
                    return IRType.I8;
                case short _:
                case ushort _:
                    return IRType.I16;
                case int _:
                case uint _:
                    return IRType.I32;
                case long _:
                case ulong _:
                    return IRType.I64;
                default:
                    throw new IRTypeException();
            }
        }

        public T Value { get; set; }

        public IRConstant(T value)
            : base(GetIRType(value))
        {
            Value = value;
        }

        public override IRExpression CloneComplete() 
            => new IRConstant<T>(Value);

        public override CExpression ToCExpression()
            => new CRawLiteral<T>(Value);
    }
}