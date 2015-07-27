using ExpressionToCodeLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Xunit;

namespace ExpressionToCodeTest
{
    public class BlockExpressionTest
    {
        [Fact]
        public void AddAssign()
        {
            var expr = Expression.AddAssign(Expression.Variable(typeof(int), "x"), Expression.Variable(typeof(int), "y"));
            Assert.Equal("x += y", ExpressionToCode.ToCode(expr));
        }

        [Fact]
        public void AddAssignChained()
        {
            var xVar = Expression.Variable(typeof(int), "x");
            var yVar = Expression.Variable(typeof(int), "y");
            var expr = Expression.AddAssign(xVar, Expression.AddAssign(xVar, yVar));
            Assert.Equal("x += (x += y)", ExpressionToCode.ToCode(expr));
            //TODO: remove redundant parens
        }

        [Fact]
        public void AndAssign()
        {
            var expr = Expression.AndAssign(Expression.Variable(typeof(int), "x"), Expression.Variable(typeof(int), "y"));
            Assert.Equal("x &= y", ExpressionToCode.ToCode(expr));
        }

        [Fact]
        public void AndAssignChained()
        {
            var xVar = Expression.Variable(typeof(int), "x");
            var yVar = Expression.Variable(typeof(int), "y");
            var expr = Expression.AndAssign(xVar, Expression.AndAssign(xVar, yVar));
            Assert.Equal("x &= (x &= y)", ExpressionToCode.ToCode(expr));
            //TODO: remove redundant parens
        }

        [Fact]
        public void PostDecrementAssign()
        {
            int x = 1, y = 4, z=8;
            var expr = Expression.PostDecrementAssign(Expression.Variable(typeof(int), "x"));
            Assert.Equal("x--", ExpressionToCode.ToCode(expr));
        }

        [Fact]
        public void PreDecrementAssign()
        {
            var expr = Expression.PreDecrementAssign(Expression.Variable(typeof(int), "x"));
            Assert.Equal("--x", ExpressionToCode.ToCode(expr));
        }

        [Fact]
        public void PostIncrementAssign()
        {
            var expr = Expression.PostIncrementAssign(Expression.Variable(typeof(int), "x"));
            Assert.Equal("x++", ExpressionToCode.ToCode(expr));
        }

        [Fact]
        public void PreIncrementAssign()
        {
            var expr = Expression.PreIncrementAssign(Expression.Variable(typeof(int), "x"));
            Assert.Equal("++x", ExpressionToCode.ToCode(expr));
        }

        [Fact]
        public void SingleStatementBlockTest()
        {
            Expression<Func<int>> v = () => 1;
            Assert.Equal(@"{ () => 1; }", ExpressionToCode.ToCode(Expression.Block(typeof(void), v)));
        }

        [Fact]
        public void BlockVariablesTest()
        {
            ParameterExpression p = Expression.Parameter(typeof(int), "p");
            ParameterExpression x = Expression.Parameter(typeof(int), "x");
            Expression assignment = Expression.Assign(p, x);
            Assert.Equal(@"{ int p; int x; p = x; }", ExpressionToCode.ToCode(Expression.Block(typeof(void), new ParameterExpression[] { p, x }, assignment)));
        }

        [Fact]
        public void BlockReturnTest() { Assert.Equal(@"{ return 1; }", ExpressionToCode.ToCode(Expression.Block(typeof(int), Expression.Constant(1)))); }

        [Fact]
        public void VoidReturnBlockTest() { Assert.Equal(@"{ 1; }", ExpressionToCode.ToCode(Expression.Block(typeof(void), Expression.Constant(1)))); }

        [Fact]
        public void MultipleStatementsBlockTest()
        {
            ParameterExpression p = Expression.Parameter(typeof(int), "p");
            Expression assignment = Expression.Assign(p, Expression.Constant(1));
            Expression addAssignment = Expression.AddAssign(p, Expression.Constant(5));
            Assert.Equal(@"{ int p; p = 1; p += 5; }", ExpressionToCode.ToCode(Expression.Block(typeof(void), new ParameterExpression[] { p }, assignment, addAssignment)));
        }

        [Fact]
        public void MultipleStatementWithReturnBlockTest()
        {
            ParameterExpression p = Expression.Parameter(typeof(Int32), "p");
            Expression assignment = Expression.Assign(p, Expression.Constant(1));
            Expression addAssignment = Expression.AddAssign(p, Expression.Constant(5));
            Assert.Equal(
                @"{ int p; p = 1; return p += 5; }",
                ExpressionToCode.ToCode(Expression.Block(typeof(Int32), new ParameterExpression[] { p }, assignment, addAssignment)));
        }
    }
}
