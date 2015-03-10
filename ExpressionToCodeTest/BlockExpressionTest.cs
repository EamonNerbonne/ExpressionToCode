using ExpressionToCodeLib;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace ExpressionToCodeTest
{
    class BlockExpressionTest
    {
        [Test]
        public void SingleStatementBlockTest()
        {
            Expression<Func<int>> v = () => 1;
            Assert.AreEqual(@"{ () => 1; }", ExpressionToCode.ToCode(Expression.Block(typeof(void), v)));
        }

        [Test]
        public void BlockVariablesTest()
        {
            ParameterExpression p = Expression.Parameter(typeof(int), "p");
            ParameterExpression x = Expression.Parameter(typeof(int), "x");
            Expression assignment = Expression.Assign(p, x);
            Assert.AreEqual(@"{ int p; int x; p = x; }", ExpressionToCode.ToCode(Expression.Block(typeof(void), new ParameterExpression[] { p, x }, assignment)));
        }

        [Test]
        public void BlockReturnTest()
        {
            Assert.AreEqual(@"{ return 1; }", ExpressionToCode.ToCode(Expression.Block(typeof(int), Expression.Constant(1))));
        }

        [Test]
        public void VoidReturnBlockTest()
        {
            Assert.AreEqual(@"{ 1; }", ExpressionToCode.ToCode(Expression.Block(typeof(void), Expression.Constant(1))));
        }

        [Test]
        public void MultipleStatementsBlockTest()
        {
            ParameterExpression p = Expression.Parameter(typeof(int), "p");
            Expression assignment = Expression.Assign(p, Expression.Constant(1));
            Expression addAssignment = Expression.AddAssign(p, Expression.Constant(5));
            Assert.AreEqual(@"{ int p; p = 1; p += 5; }", ExpressionToCode.ToCode(Expression.Block(typeof(void), new ParameterExpression[] { p }, assignment, addAssignment)));
        }

        [Test]
        public void MultipleStatementWithReturnBlockTest()
        {
            ParameterExpression p = Expression.Parameter(typeof(Int32), "p");
            Expression assignment = Expression.Assign(p, Expression.Constant(1));
            Expression addAssignment = Expression.AddAssign(p, Expression.Constant(5));
            Assert.AreEqual(@"{ int p; p = 1; return p += 5; }", ExpressionToCode.ToCode(Expression.Block(typeof(Int32), new ParameterExpression[] { p }, assignment, addAssignment)));
        }
    }
}
