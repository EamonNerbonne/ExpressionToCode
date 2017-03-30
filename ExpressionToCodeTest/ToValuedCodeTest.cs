using System;
using System.Collections.Generic;
using System.Linq;
using ExpressionToCodeLib;
using Xunit;

namespace ExpressionToCodeTest
{
    public class ToValuedCodeTest
    {
        [Fact]
        public void ToValuedCode_ofNull_fails()
        {
            ExpressionToCodeLibTest obj = null;

            Assert.ThrowsAny<InvalidOperationException>(
                () =>
                    ExpressionToCode.ToValuedCode(() => obj.TheProperty));
        }

        [Fact]
        public void ArrayLength()
        {
            var arr = new[] { 1 };
            var actual = ExpressionToCode.ToValuedCode(() => arr.Length);
            Assert.Equal("arr.Length = 1", actual);
        }

        [Fact]
        public void ThePropertyAccess()
        {
            var actual = ExpressionToCode.ToValuedCode(() => TheProperty);
            Assert.Equal("ToValuedCodeTest.TheProperty = TheValue", actual);
        }

        [Fact]
        public void TheStringVariable()
        {
            var theVariable = "theValue";
            var actual = ExpressionToCode.ToValuedCode(() => theVariable);
            Assert.Equal("theVariable = theValue", actual);
        }

        [Fact]
        public void TheMethod()
        {
            var actual = ExpressionToCode.ToValuedCode(() => TheMethod(1, "2"));
            Assert.Equal("TheMethod(1, \"2\") = TheMethod 1 2", actual);
        }

        [Fact]
        public void TheGenericMethod()
        {
            var actual = ExpressionToCode.ToValuedCode(() => TheGenericMethod<int>(2));
            Assert.Equal("TheGenericMethod<int>(2) = Return value is 4", actual);
        }

        [Fact]
        public void ThisIndexedProperty()
        {
            var actual = ExpressionToCode.ToValuedCode(() => this[1]);
            Assert.Equal("this[1] = TheIndexedValue", actual);
        }

        [Fact]
        public void ThisMethodCall()
        {
            var code = ExpressionToCode.ToValuedCode(() => ReturnZero());

            Assert.Equal("ReturnZero() = 0", code);
        }

        [Fact]
        public void ThisStaticMethodCall()
        {
            var code = ExpressionToCode.ToValuedCode(() => StaticReturnZero());

            Assert.Equal("ToValuedCodeTest.StaticReturnZero() = 0", code);
        }

        static string TheProperty => "TheValue";

        // ReSharper disable once UnusedParameter.Local
        string this[int index] => "TheIndexedValue";

        static int StaticReturnZero()
            => 0;

        // ReSharper disable MemberCanBeMadeStatic.Local
        string TheMethod(int parameter1, string parameter2)
            => "TheMethod " + parameter1 + " " + parameter2;

        // ReSharper disable once UnusedTypeParameter
        string TheGenericMethod<T>(int two)
            => "Return value is " + two * two;

        int ReturnZero()
            => 0;
    }
}
