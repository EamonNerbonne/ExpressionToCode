using System;
using System.Linq.Expressions;
using Xunit;

namespace ExpressionToCodeTest
{
    public class ExpressionInterpretationBug
    {
        struct AStruct
        {
            public int AValue;
        }

        class SomethingMutable
        {
            public AStruct AStructField;
        }

        static Expression<Func<T>> Expr<T>(Expression<Func<T>> e)
            => e;

        [Fact]
        public void DotNetCoreIsNotBuggy()
        {
            var expr_compiled = Expr(() => new SomethingMutable { AStructField = { AValue = 2 } }).Compile(false)();
            Assert.Equal(2, expr_compiled.AStructField.AValue);

            var expr_interpreted = Expr(() => new SomethingMutable { AStructField = { AValue = 2 } }).Compile(true)();
            Assert.Equal(2, expr_interpreted.AStructField.AValue);
        }
    }
}
