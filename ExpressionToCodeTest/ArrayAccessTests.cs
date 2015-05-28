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
        Expression GenerateCode<T1,T2>(Expression<Func<T1,T2>> code) {
            return code;    
        }
        [Test]
        public void Test001()
        {
            Assert.AreEqual("a => a[1]",
                ExpressionToCode.ToCode(
                GenerateCode<string[],string>(a=>a[1]))
            );
        }
        [Test]
        public void Test002()
        {
            var param = Expression.Parameter(typeof(string).MakeArrayType(), "a");
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
