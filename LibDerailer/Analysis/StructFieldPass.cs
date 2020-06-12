using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.IR;
using LibDerailer.IR.Expressions;
using LibDerailer.IR.Instructions;
using LibDerailer.IR.Types;

namespace LibDerailer.Analysis
{
    public class StructFieldPass : AnalysisPass
    {
        private IRExpression SubstituteFieldAccess(IRExpression expression, int bitOffset, int bitCount)
        {
            // if (!(expression is IRDerefExpression deref))
            //     return null;
            if (!(expression.Type is IRPointer ptrType))
                return null;
            if (!(expression is IRConversionExpression conv))
                return null;

            var derefType = ptrType.BaseType;

            if (conv.Operand is IRBinaryExpression bin && bin.Operator == IRBinaryOperator.Add)
            {
                IRExpression basePointer = null;
                IRExpression offset      = null;
                if (bin.OperandA.Type is IRPointer p3 && p3.BaseType.GetRootType() is IRStruct)
                {
                    basePointer = bin.OperandA;
                    offset      = bin.OperandB;
                }
                else if (bin.OperandA is IRConversionExpression c5 &&
                         c5.Operand.Type is IRPointer p5 &&
                         p5.BaseType.GetRootType() is IRStruct)
                {
                    basePointer = c5.Operand;
                    offset      = bin.OperandB;
                }
                else if (bin.OperandB.Type is IRPointer p4 && p4.BaseType.GetRootType() is IRStruct)
                {
                    basePointer = bin.OperandB;
                    offset      = bin.OperandA;
                }
                else if (bin.OperandB is IRConversionExpression c6 &&
                         c6.Operand.Type is IRPointer p6 &&
                         p6.BaseType.GetRootType() is IRStruct)
                {
                    basePointer = c6.Operand;
                    offset      = bin.OperandA;
                }

                if (!(basePointer is null) && offset is IRConstant<uint> constOffset)
                {
                    if (basePointer is IRVariable v)
                    {
                        var type = (IRStruct) ((IRPointer) v.Type).BaseType.GetRootType();
                        var fields = type.Fields.Where(f =>
                                f.Offset <= constOffset.Value && f.Offset + f.Type.ByteSize >=
                                constOffset.Value + derefType.ByteSize)
                            .ToArray();
                        if (bitCount > 0)
                        {
                            var fields2 = fields.Where(c =>
                                c.BitCount == bitCount && c.BitOffset == bitOffset &&
                                c.Offset == constOffset.Value && c.Type.ByteSize == derefType.ByteSize).ToArray();
                            if (fields2.Length == 1)
                            {
                                return new IRStructFieldExpression(basePointer, fields2[0], null).Cast(derefType);
                            }
                        }
                        else
                        {
                            if (fields.Length == 1 && fields[0].Offset == constOffset.Value &&
                                fields[0].Type.ByteSize == derefType.ByteSize)
                            {
                                return new IRStructFieldExpression(basePointer, fields[0], null).Cast(derefType);
                            }
                            else if (fields.Length == 1)
                            {
                                //this is either some weird type conversion, or a struct in a struct
                                if(fields[0].Type.GetRootType() is IRStruct s)
                                {
                                    IRStruct curStruct = s;
                                    var structField = new IRStructFieldExpression(basePointer, fields[0], null);
                                    uint fieldOffset = constOffset.Value - fields[0].Offset;
                                    while (true)
                                    {
                                        var fields2 = curStruct.Fields.Where(f =>
                                                f.Offset <= fieldOffset && f.Offset + f.Type.ByteSize >=
                                                fieldOffset + derefType.ByteSize)
                                            .ToArray();
                                        if (fields2.Length == 1 && fields2[0].Offset == fieldOffset &&
                                            fields2[0].Type.ByteSize == derefType.ByteSize)
                                        {
                                            return new IRStructFieldExpression(structField, fields2[0], null).Cast(
                                                derefType);
                                        }
                                        else if (fields2.Length == 1)
                                        {
                                            if (fields2[0].Type.GetRootType() is IRStruct s2)
                                            {
                                                structField = new IRStructFieldExpression(structField, fields2[0], null);
                                                fieldOffset -= fields2[0].Offset;
                                                curStruct = s2;
                                            }
                                        }
                                        else
                                            break;
                                    }
                                }
                            }
                            else if (fields.Length > 1)
                            {
                                //this may be a bitfield
                                var fields2 = fields.Where(c =>
                                        c.BitOffset ==
                                        (constOffset.Value - c.Offset) * 8 &&
                                        c.BitCount == derefType.ByteSize * 8 &&
                                        c.Type.ByteSize >= derefType.ByteSize +
                                        (constOffset.Value - c.Offset))
                                    .ToArray();
                                if (fields2.Length == 1)
                                {
                                    return new IRStructFieldExpression(basePointer, fields2[0], null).Cast(derefType);
                                }
                            }
                        }

                        // var fieldExpr = ((IRStruct)((IRPointer)v.Type).BaseType.GetRootType()).GetFieldCExpression(v, constOffset.Value, deref.Type.ByteSize);
                        // if (!(fieldExpr is null))
                        //     return fieldExpr;
                    }
                }
            }

            return null;
        }

        public override void Run(IRContext context)
        {
            var structBaseType            = new IRMatchType(type => type.GetRootType() is IRStruct);
            var structPointerVar          = new IRRegisterVariable(structBaseType.GetPointer(), "structPointerVar");
            var fieldType                 = new IRMatchType(type => true);
            var substVar                  = new IRRegisterVariable(fieldType, "");
            var displacementVar           = new IRRegisterVariable(IRPrimitive.U32, "disp");
            var derefExprVar              = new IRRegisterVariable(fieldType, "derefExprVar");
            var leftShiftAmount           = new IRRegisterVariable(IRPrimitive.S32, "a");
            var rightShiftAmount          = new IRRegisterVariable(IRPrimitive.S32, "b");
            var shiftDerefExprVarUnsigned = new IRRegisterVariable(fieldType, "derefExprVar");
            var shiftDerefExprUnsigned = shiftDerefExprVarUnsigned.ShiftLeft(leftShiftAmount.Cast(IRPrimitive.U32))
                .ShiftRightLogical(rightShiftAmount.Cast(IRPrimitive.U32));
            foreach (var basicBlock in context.Function.BasicBlocks)
            {
                foreach (var instruction in basicBlock.Instructions)
                {
                    instruction.Substitute(derefExprVar, substVar, map =>
                    {
                        if (!(map[derefExprVar] is IRDerefExpression deref))
                            return false;
                        var subst = SubstituteFieldAccess(deref.Pointer, 0, 0);
                        if (!(subst is null))
                        {
                            map[substVar] = subst;
                            return true;
                        }

                        return false;
                    });

                    instruction.Substitute(shiftDerefExprUnsigned, substVar, map =>
                    {
                        if (!(map[shiftDerefExprVarUnsigned] is IRDerefExpression deref) ||
                            !(map[leftShiftAmount] is IRConstant<int> a) ||
                            !(map[rightShiftAmount] is IRConstant<int> b))
                            return false;

                        int bitCount  = 32 - b.Value;
                        int bitOffset = 32 - bitCount - a.Value;

                        var subst = SubstituteFieldAccess(deref.Pointer, bitOffset, bitCount);
                        if (!(subst is null))
                        {
                            map[substVar] = subst;
                            return true;
                        }

                        return false;
                    });
                }
            }

            // foreach (var basicBlock in context.Function.BasicBlocks)
            //     foreach (var instruction in basicBlock.Instructions)
            //         instruction.Substitute(
            //             new IRDerefExpression(fieldType,
            //                 new IRConversionExpression(fieldType.GetPointer(), structPointerVar + displacementVar)),
            //             substVar,
            //             map =>
            //             {
            //                 return false;
            //             });
        }
    }
}