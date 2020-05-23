using System;
using System.Collections.Generic;
using Gee.External.Capstone.Arm;
using LibDerailer.CCodeGen;
using LibDerailer.CCodeGen.Statements;
using LibDerailer.CCodeGen.Statements.Expressions;
using LibDerailer.CodeGraph;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Variable = LibDerailer.CCodeGen.Statements.Expressions.Variable;

namespace LibDerailerTest
{
    [TestClass]
    public class ASTPrinterTests
    {
        [TestMethod]
        public void PrintSimpleFunctionTest()
        {
            var ifStatement = new If((Expression) 5 != 3.2f, new Block(
                new Label("Start"),
                new While(true),
                new Goto("Start"),
                Expression.Assign(new Variable("Something")[5], "Thing")
            ));
            var procedure = new Method("test_a", (new TypeName("void", true), "a"))
            {
                Body = new Block(ifStatement)
            };

            Console.WriteLine(procedure);
        }
    }
}