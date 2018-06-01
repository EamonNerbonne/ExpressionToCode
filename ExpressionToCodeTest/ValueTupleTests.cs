using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using ExpressionToCodeLib;
using Xunit;

namespace ExpressionToCodeTest {
    public class ValueTupleTests {
        [Fact]
        public void ExpressionCompileValueTupleEqualsWorks() {
            var tuple = (1, 3);
            var tuple2 = (1, "123".Length);
            Expression<Func<bool>> expr = () => tuple.Equals(tuple2);
            Assert.True(expr.Compile()());
        }

        [Fact]
        public void FastExpressionCompileValueTupleEqualsWorks() {
            var tuple = (1, 3);
            (int, int Length) tuple2 = (1, "123".Length);
            ValueTuple<int,int> x;
            var expr = FastExpressionCompiler.ExpressionCompiler.Compile(() => tuple.Equals(tuple2));
            Assert.True(expr());
        }

        [Fact]
        public void AssertingOnValueTupleEqualsWorks() {
            var tuple = (1, 3);
            var tuple2 = (1, "123".Length);
            Expression<Func<bool>> expr = () => tuple.Equals(tuple2);
            Assert.True(expr.Compile()());
            PAssert.That(() => tuple.Equals(tuple2));
        }
    }
}
