using ExpressionToCodeTest.Unstable_v2_Api;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ExpressionToCodeTest
{
    public class ToValuedCodeTest
    {
        [Fact]
        public void ToValuedCode_ofNull_fails()
        {
            ExpressionToCodeTest obj = null;
            
            Assert.ThrowsAny<InvalidOperationException>(
                () =>
                    ExpressionWithValue.ToValuedCode(() => obj.TheProperty));
        }

        [Fact]
        public void ArrayLength()
        {
            var arr = new[] { 1 };
            var actual = ExpressionWithValue.ToValuedCode(() => arr.Length);
            Assert.Equal("arr.Length = 1", actual);
        }

        [Fact]
        public void ThePropertyAccess()
        {
            var actual = ExpressionWithValue.ToValuedCode(() => TheProperty);
            Assert.Equal("TheProperty = TheValue", actual);
        }

        [Fact]
        public void TheStringVariable()
        {
            var theVariable = "theValue";
            var actual = ExpressionWithValue.ToValuedCode(() => theVariable);
            Assert.Equal("theVariable = theValue", actual);
        }

        [Fact]
        public void TheMethod()
        {
            var actual = ExpressionWithValue.ToValuedCode(() => TheMethod(1, "2"));
            Assert.Equal("TheMethod(1, \"2\") = TheMethod 1 2", actual);
        }

        [Fact]
        public void TheGenericMethod()
        {
            var actual = ExpressionWithValue.ToValuedCode(() => TheGenericMethod<int>(2));
            Assert.Equal("TheGenericMethod<Int32>(2) = Return value is 4", actual);
        }

        [Fact]
        public void ThisIndexedProperty()
        {
            var actual = ExpressionWithValue.ToValuedCode(() => this[1]);
            Assert.Equal("ToValuedCodeTest[1] = TheIndexedValue", actual);
        }

        [Fact]
        public void ThisMethodCall()
        {
            var code = ExpressionWithValue.ToValuedCode(() => ReturnZero());

            Assert.Equal("ReturnZero() = 0", code);
        }

        [Fact]
        public void ThisStaticMethodCall()
        {
            var code = ExpressionWithValue.ToValuedCode(() => StaticReturnZero());

            Assert.Equal("ToValuedCodeTest.StaticReturnZero() = 0", code);
        }

        static string TheProperty { get { return "TheValue"; } }
        // ReSharper disable once UnusedParameter.Local
        string this[int index] { get { return "TheIndexedValue"; } }
        static int StaticReturnZero() { return 0; }
        // ReSharper disable MemberCanBeMadeStatic.Local
        string TheMethod(int parameter1, string parameter2) { return "TheMethod " + parameter1 + " " + parameter2; }
        // ReSharper disable once UnusedTypeParameter
        string TheGenericMethod<T>(int two) { return "Return value is " + two * two; }
        int ReturnZero() { return 0; }
    }
}
