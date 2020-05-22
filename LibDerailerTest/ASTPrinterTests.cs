using System;
using System.Collections.Generic;
using LibDerailer.CodeGraph.Nodes.CCodeGen;
using Gee.External.Capstone.Arm;
using LibDerailer.CodeGraph;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibDerailerTest
{
    [TestClass]
    public class ASTPrinterTests
    {
        [TestMethod]
        public void PrintSimpleFunctionTest()
        {
            var if_statement = new IfStatement
            {
                predicate = new ProcedureCall
                {
                    signature = "!=",
                    is_operator = true,
                    arguments = new List<Expression>
                    {
                        new RawLiteral<int>(5),
                        new RawLiteral<float>(3.2f)
                    }
                },
                if_body = new Block
                {
                    statements = new List<Statement>
                    {
                        new LabelStatement{ label = "Start" },
                        new WhileStatement
                        {
                            predicate = new RawLiteral<int>(1)
                        },
                        new GotoStatement{ label = "Start" },
                        new ProcedureCall
                        {
                            signature = "=",
                            is_operator = true,
                            arguments = new List<Expression>
                            {
                                new ProcedureCall
                                {
                                    signature = "[]",
                                    is_operator = true,
                                    arguments = new List<Expression>
                                    {
                                        new RawLiteral<string>("Something"),
                                        new RawLiteral<int>(5)
                                    }
                                },
                                new StringLiteral("Thing")
                            }
                        }
                    }
                }
            };
            var procedure = new Procedure
            {
                signature = "test_a",
                parameters = new List<Tuple<TypeName, string>>
                {
                    new Tuple<TypeName, string>(new TypeName { is_pointer = true }, "a")
                },
                body = new Block
                {
                    statements = new List<Statement>
                    {
                       if_statement
                    }
                }
            };

            System.Console.WriteLine(procedure);
        }
    }
}