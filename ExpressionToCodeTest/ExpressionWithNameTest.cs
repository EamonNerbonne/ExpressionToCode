using System;
using System.Linq;
using System.Linq.Expressions;
using ExpressionToCodeLib;
using Xunit;

namespace ExpressionToCodeTest
{
    public class ExpressionWithNameTest
    {
        [Fact]
        public void TheVariable_ToNameOf()
        {
            var theVariable = "theValue";
            var actual = ExpressionToCode.GetNameIn(() => theVariable);
            Assert.Equal("theVariable", actual);
        }

        [Fact]
        public void TheMethod_ToNameOf()
        {
            var x = 1;
            var y = "";
            var actual = ExpressionToCode.GetNameIn(() => TheComplexMethod(x, y));
            Assert.Equal("TheComplexMethod", actual);
        }

        [Fact]
        public void TheMethod_ToNameOf_asVariable()
        {
            Expression<Func<int, string, string>> theComplexMethod = (x, y) => TheComplexMethod(x, y);
            var actual = ExpressionToCode.GetNameIn(theComplexMethod);
            Assert.Equal("TheComplexMethod", actual);

            var full = ExpressionToCode.ToCode(theComplexMethod.Body);
            Assert.NotEqual(full, actual);
        }

        [Fact]
        public void TheMethod_ToNameOf_withValues()
        {
            var actual = ExpressionToCode.GetNameIn(() => TheComplexMethod(1, "2"));
            Assert.Equal("TheComplexMethod", actual);
        }

        [Fact]
        public void TheGenericMethod_ToNameOf()
        {
            var actual = ExpressionToCode.GetNameIn(() => TheGenericMethod<int>(2));
            Assert.Equal("TheGenericMethod", actual);
        }

        [Fact]
        public void TheProperty_ToNameOf()
        {
            var actual = ExpressionToCode.GetNameIn(() => TheProperty);
            Assert.Equal("TheProperty", actual);
        }

        [Fact]
        public void TheSimpleMethod_ToNameOf()
        {
            var actual = ExpressionToCode.GetNameIn(() => TheSimpleMethod());
            Assert.Equal("TheSimpleMethod", actual);
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        void TheSimpleMethod() { }

        static string TheProperty => "TheValue";

        static string TheComplexMethod(int parameter1, string parameter2) => "TheMethod " + parameter1 + " " + parameter2;

        static string TheGenericMethod<T>(int two) => "Return value is " + two * two + typeof(T).Name;
    }
}
