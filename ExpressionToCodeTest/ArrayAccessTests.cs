using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using ExpressionToCodeLib;
using NUnit.Framework;

namespace ExpressionToCodeTest
{
    class ArrayAccessTests
    {
        [Test]
        public void TestSingleDimensionalArrayIndexExpressionWithLambda()
        {
            var param = Expression.Parameter(typeof(string[]), "a");
            var expr = Expression.Lambda(
                Expression.ArrayIndex(param, Expression.Constant(1)),
                param
            );
            Assert.AreEqual(
				"a => a[1]",
                ExpressionToCode.ToCode(expr)
            );
        }
        [Test]
        public void TestSingleDimensionalArrayAccessExpressionWithLambda()
        {
            var param = Expression.Parameter(typeof(string[]), "a");
            var expr = Expression.Lambda(
                Expression.ArrayAccess(param, Expression.Constant(1)),
                param
            );
            Assert.AreEqual(
                "a => a[1]",
                ExpressionToCode.ToCode(expr)
            );
        }
    }
}
