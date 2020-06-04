using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDerailer.IR.Expressions;
using LibDerailer.IR.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibDerailerTest
{
    [TestClass]
    public class UnificationTests
    {
        [TestMethod]
        public void ConstConstEqTest()
        {
            IRExpression expression = 3;
            IRExpression template   = 3;
            var          mapping    = new Dictionary<IRVariable, IRExpression>();
            Assert.IsTrue(expression.Unify(template, mapping));
            Assert.AreEqual(0, mapping.Count);
        }

        [TestMethod]
        public void ConstConstNeTest()
        {
            IRExpression expression = 3;
            IRExpression template   = 7;
            var          mapping    = new Dictionary<IRVariable, IRExpression>();
            Assert.IsFalse(expression.Unify(template, mapping));
        }

        [TestMethod]
        public void VarConstTest()
        {
            IRExpression expression   = 3;
            var          templateVarA = new IRRegisterVariable(IRPrimitive.S32, "a");
            var          template     = templateVarA;
            var          mapping      = new Dictionary<IRVariable, IRExpression>();
            Assert.IsTrue(expression.Unify(template, mapping));
            Assert.AreEqual(1, mapping.Count);
            Assert.AreEqual(mapping[templateVarA], expression);
        }

        [TestMethod]
        public void VarVarTest()
        {
            var exprVar      = new IRRegisterVariable(IRPrimitive.S32, "exprVar");
            var expression   = exprVar;
            var templateVarA = new IRRegisterVariable(IRPrimitive.S32, "a");
            var template     = templateVarA;
            var mapping      = new Dictionary<IRVariable, IRExpression>();
            Assert.IsTrue(expression.Unify(template, mapping));
            Assert.AreEqual(1, mapping.Count);
            Assert.AreEqual(mapping[templateVarA], exprVar);
        }

        [TestMethod]
        public void MultiVarEqTest()
        {
            var exprVar    = new IRRegisterVariable(IRPrimitive.S32, "exprVar");
            var expression = exprVar + exprVar;
            var templateVarA = new IRRegisterVariable(IRPrimitive.S32, "a");
            var template     = templateVarA + templateVarA;
            var mapping      = new Dictionary<IRVariable, IRExpression>();
            Assert.IsTrue(expression.Unify(template, mapping));
            Assert.AreEqual(1, mapping.Count);
            Assert.AreEqual(mapping[templateVarA], exprVar);
        }

        [TestMethod]
        public void MultiVarEqTest2()
        {
            IRExpression irConst3     = 3;
            IRExpression expression   = irConst3 + irConst3;
            var          templateVarA = new IRRegisterVariable(IRPrimitive.S32, "a");
            var          template     = templateVarA + templateVarA;
            var          mapping      = new Dictionary<IRVariable, IRExpression>();
            Assert.IsTrue(expression.Unify(template, mapping));
            Assert.AreEqual(1, mapping.Count);
            Assert.AreEqual(mapping[templateVarA], irConst3);
        }

        [TestMethod]
        public void MultiVarNeTest()
        {
            var exprVar      = new IRRegisterVariable(IRPrimitive.S32, "exprVar");
            var exprVar2      = new IRRegisterVariable(IRPrimitive.S32, "exprVar2");
            var expression   = exprVar + exprVar2;
            var templateVarA = new IRRegisterVariable(IRPrimitive.S32, "a");
            var template     = templateVarA + templateVarA;
            var mapping      = new Dictionary<IRVariable, IRExpression>();
            Assert.IsFalse(expression.Unify(template, mapping));
        }

        [TestMethod]
        public void MultiVarNeTest2()
        {
            IRExpression irConst3     = 3;
            IRExpression irConst2     = 2;
            IRExpression expression   = irConst3 + irConst2;
            var          templateVarA = new IRRegisterVariable(IRPrimitive.S32, "a");
            var          template     = templateVarA + templateVarA;
            var          mapping      = new Dictionary<IRVariable, IRExpression>();
            Assert.IsFalse(expression.Unify(template, mapping));
        }

        [TestMethod]
        public void MultiVarGenericTemplateTest()
        {
            var exprVar      = new IRRegisterVariable(IRPrimitive.S32, "exprVar");
            var expression   = exprVar + exprVar;
            var templateVarA = new IRRegisterVariable(IRPrimitive.S32, "a");
            var templateVarB = new IRRegisterVariable(IRPrimitive.S32, "b");
            var template     = templateVarA + templateVarB;
            var mapping      = new Dictionary<IRVariable, IRExpression>();
            Assert.IsTrue(expression.Unify(template, mapping));
            Assert.AreEqual(2, mapping.Count);
            Assert.AreEqual(mapping[templateVarA], exprVar);
            Assert.AreEqual(mapping[templateVarB], exprVar);
        }
    }
}