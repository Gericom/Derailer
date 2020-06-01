using System;
using LibDerailer.CCodeGen.Statements.Expressions;
using LibDerailer.IR.Types;

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
                    return IRPrimitive.Bool;
                case sbyte _:
                    return IRPrimitive.S8;
                case byte _:
                    return IRPrimitive.U8;
                case short _:
                    return IRPrimitive.S16;
                case ushort _:
                    return IRPrimitive.U16;
                case int _:
                    return IRPrimitive.S32;
                case uint _:
                    return IRPrimitive.U32;
                case long _:
                    return IRPrimitive.S64;
                case ulong _:
                    return IRPrimitive.U64;
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

        public override bool Equals(object obj)
            => obj is IRConstant<T> exp &&
               exp.Value.Equals(Value) &&
               exp.Type == Type;
    }
}