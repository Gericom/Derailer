﻿using System;
using System.Collections.Generic;
using System.Security.Policy;
using LibDerailer.CCodeGen.Statements.Expressions;

namespace LibDerailer.IR.Expressions
{
    public abstract class IRExpression
    {
        public IRType Type { get; }

        public IRExpression(IRType type)
        {
            Type = type;
        }

        public virtual void Substitute(IRVariable variable, IRExpression expression)
        {
        }

        public virtual HashSet<IRVariable> GetAllVariables()
            => new HashSet<IRVariable>();

        public abstract CExpression ToCExpression();

        public IRExpression InverseCondition()
        {
            if (Type != IRType.I1)
                throw new IRTypeException();
            if (!(this is IRComparisonExpression ce))
                return !this;
            IRComparisonOperator revOp;
            switch (ce.Operator)
            {
                case IRComparisonOperator.Equal:
                    revOp = IRComparisonOperator.NotEqual;
                    break;
                case IRComparisonOperator.NotEqual:
                    revOp = IRComparisonOperator.Equal;
                    break;
                case IRComparisonOperator.Less:
                    revOp = IRComparisonOperator.GreaterEqual;
                    break;
                case IRComparisonOperator.LessEqual:
                    revOp = IRComparisonOperator.Greater;
                    break;
                case IRComparisonOperator.Greater:
                    revOp = IRComparisonOperator.LessEqual;
                    break;
                case IRComparisonOperator.GreaterEqual:
                    revOp = IRComparisonOperator.Less;
                    break;
                case IRComparisonOperator.UnsignedLess:
                    revOp = IRComparisonOperator.UnsignedGreaterEqual;
                    break;
                case IRComparisonOperator.UnsignedLessEqual:
                    revOp = IRComparisonOperator.UnsignedGreater;
                    break;
                case IRComparisonOperator.UnsignedGreater:
                    revOp = IRComparisonOperator.UnsignedLessEqual;
                    break;
                case IRComparisonOperator.UnsignedGreaterEqual:
                    revOp = IRComparisonOperator.UnsignedLess;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return new IRComparisonExpression(revOp, ce.OperandA, ce.OperandB);
        }

        public IRConversionExpression Sext(IRType dstType)
            => new IRConversionExpression(dstType, IRConversionOperator.Sext, this);

        public IRConversionExpression Zext(IRType dstType)
            => new IRConversionExpression(dstType, IRConversionOperator.Zext, this);

        public IRConversionExpression Trunc(IRType dstType)
            => new IRConversionExpression(dstType, IRConversionOperator.Trunc, this);

        public IRBinaryExpression ShiftLeft(IRExpression amount)
            => new IRBinaryExpression(Type, IRBinaryOperator.Lsl, this, amount);

        public IRBinaryExpression ShiftRightLogical(IRExpression amount)
            => new IRBinaryExpression(Type, IRBinaryOperator.Lsr, this, amount);

        public IRBinaryExpression ShiftRightArithmetic(IRExpression amount)
            => new IRBinaryExpression(Type, IRBinaryOperator.Asr, this, amount);

        public IRComparisonExpression LessThan(IRExpression b)
            => new IRComparisonExpression(IRComparisonOperator.Less, this, b);

        public IRComparisonExpression UnsignedLessThan(IRExpression b)
            => new IRComparisonExpression(IRComparisonOperator.UnsignedLess, this, b);

        public IRComparisonExpression LessEqualAs(IRExpression b)
            => new IRComparisonExpression(IRComparisonOperator.LessEqual, this, b);

        public IRComparisonExpression UnsignedLessEqualAs(IRExpression b)
            => new IRComparisonExpression(IRComparisonOperator.UnsignedLessEqual, this, b);

        public IRComparisonExpression GreaterThan(IRExpression b)
            => new IRComparisonExpression(IRComparisonOperator.Greater, this, b);

        public IRComparisonExpression UnsignedGreaterThan(IRExpression b)
            => new IRComparisonExpression(IRComparisonOperator.UnsignedGreater, this, b);

        public IRComparisonExpression GreaterEqualAs(IRExpression b)
            => new IRComparisonExpression(IRComparisonOperator.GreaterEqual, this, b);

        public IRComparisonExpression UnsignedGreaterEqualAs(IRExpression b)
            => new IRComparisonExpression(IRComparisonOperator.UnsignedGreaterEqual, this, b);

        public static implicit operator IRExpression(bool a)
            => new IRConstant<bool>(a);

        public static implicit operator IRExpression(sbyte a)
            => new IRConstant<sbyte>(a);

        public static implicit operator IRExpression(byte a)
            => new IRConstant<byte>(a);

        public static implicit operator IRExpression(short a)
            => new IRConstant<short>(a);

        public static implicit operator IRExpression(ushort a)
            => new IRConstant<ushort>(a);

        public static implicit operator IRExpression(int a)
            => new IRConstant<int>(a);

        public static implicit operator IRExpression(uint a)
            => new IRConstant<uint>(a);

        public static implicit operator IRExpression(long a)
            => new IRConstant<long>(a);

        public static implicit operator IRExpression(ulong a)
            => new IRConstant<ulong>(a);

        public static IRComparisonExpression operator ==(IRExpression a, IRExpression b)
            => new IRComparisonExpression(IRComparisonOperator.Equal, a, b);

        public static IRComparisonExpression operator !=(IRExpression a, IRExpression b)
            => new IRComparisonExpression(IRComparisonOperator.NotEqual, a, b);

        // public static IRComparisonExpression operator <(IRExpression a, IRExpression b)
        //     => new IRComparisonExpression(IRComparisonOperator.Less, a, b);
        //
        // public static IRComparisonExpression operator >(IRExpression a, IRExpression b)
        //     => new IRComparisonExpression(IRComparisonOperator.Greater, a, b);
        //
        // public static IRComparisonExpression operator <=(IRExpression a, IRExpression b)
        //     => new IRComparisonExpression(IRComparisonOperator.LessEqual, a, b);
        //
        // public static IRComparisonExpression operator >=(IRExpression a, IRExpression b)
        //     => new IRComparisonExpression(IRComparisonOperator.GreaterEqual, a, b);

        public static IRBinaryExpression operator +(IRExpression a, IRExpression b)
            => new IRBinaryExpression(a.Type, IRBinaryOperator.Add, a, b);

        public static IRBinaryExpression operator -(IRExpression a, IRExpression b)
            => new IRBinaryExpression(a.Type, IRBinaryOperator.Sub, a, b);

        public static IRBinaryExpression operator *(IRExpression a, IRExpression b)
            => new IRBinaryExpression(a.Type, IRBinaryOperator.Mul, a, b);

        public static IRBinaryExpression operator /(IRExpression a, IRExpression b)
            => new IRBinaryExpression(a.Type, IRBinaryOperator.Div, a, b);

        public static IRBinaryExpression operator %(IRExpression a, IRExpression b)
            => new IRBinaryExpression(a.Type, IRBinaryOperator.Mod, a, b);

        public static IRBinaryExpression operator &(IRExpression a, IRExpression b)
            => new IRBinaryExpression(a.Type, IRBinaryOperator.And, a, b);

        public static IRBinaryExpression operator |(IRExpression a, IRExpression b)
            => new IRBinaryExpression(a.Type, IRBinaryOperator.Or, a, b);

        public static IRBinaryExpression operator ^(IRExpression a, IRExpression b)
            => new IRBinaryExpression(a.Type, IRBinaryOperator.Xor, a, b);

        public static IRUnaryExpression operator -(IRExpression a)
            => new IRUnaryExpression(a.Type, IRUnaryOperator.Neg, a);

        public static IRUnaryExpression operator !(IRExpression a)
        {
            if (a.Type != IRType.I1)
                throw new IRTypeException();

            return new IRUnaryExpression(IRType.I1, IRUnaryOperator.Not, a);
        }

        public static IRUnaryExpression operator ~(IRExpression a)
            => new IRUnaryExpression(a.Type, IRUnaryOperator.Not, a);
    }
}