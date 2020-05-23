using System;
using System.Collections.Generic;
using Gee.External.Capstone.Arm;
using LibDerailer.CCodeGen;
using LibDerailer.CCodeGen.Statements;
using LibDerailer.CCodeGen.Statements.Expressions;
using LibDerailer.CodeGraph;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CVariable = LibDerailer.CCodeGen.Statements.Expressions.CVariable;

namespace LibDerailerTest
{
    [TestClass]
    public class ASTPrinterTests
    {
        [TestMethod]
        public void PrintSimpleFunctionTest()
        {
            var ifStatement = new CIf((CExpression) 5 != 3.2f, new CBlock(
                new CLabel("Start"),
                new CWhile(true),
                new CGoto("Start"),
                CExpression.Assign(new CVariable("Something")[5], "Thing")
            ));
            var procedure = new CMethod("test_a", (new CType("void", true), "a"), (new CType("int"), "b"))
            {
                IsStatic = true,
                Body = new CBlock(ifStatement)
            };

            Console.WriteLine(procedure);
        }
    }
}