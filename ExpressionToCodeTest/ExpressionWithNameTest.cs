using System;
using ExpressionToCodeLib.Unstable_v2_Api;
using Xunit;
using System.Linq;
using System.Linq.Expressions;

namespace ExpressionToCodeTest
{
    public class ExpressionWithNameTest
    {
        [Fact]
        public void TheVariable_ToNameOf()
        {
            var theVariable = "theValue";
            var actual = ExpressionWithName.ToNameOf(() => theVariable);
            Assert.Equal("theVariable", actual);
        }

        [Fact]
        public void TheMethod_ToNameOf()
        {
            var x = 1;
            var y = "";
            var actual = ExpressionWithName.ToNameOf(() => TheComplexMethod(x, y));
            Assert.Equal("TheComplexMethod", actual);
        }

        [Fact]
        public void TheMethod_ToNameOf_asVariable()
        {
            Expression<Func<int, string, string>> theComplexMethod = (x, y) => TheComplexMethod(x, y);
            var actual = ExpressionWithName.ToNameOf(theComplexMethod);
            Assert.Equal("TheComplexMethod", actual);

            var full = ExpressionToCodeLib.ExpressionToCode.ToCode(theComplexMethod.Body);
            Assert.NotEqual(full, actual);
        }

        [Fact]
        public void TheMethod_ToNameOf_withValues()
        {
            var actual = ExpressionWithName.ToNameOf(() => TheComplexMethod(1, "2"));
            Assert.Equal("TheComplexMethod", actual);
        }

        //[Fact]
        //public void TheComplexMethod_ToFullNameOf()
        //{
        //	Expression<Func<int, string, string>> theComplexMethod = (x, y) => ExpressionWithNameTest.TheComplexMethod(1, "");
        //	var actual = ExpressionWithName.ToFullNameOf(theComplexMethod);
        //	Assert.Equal("ExpressionWithNameTest.TheComplexMethod(parameter1, parameter2)", actual);

        //	var full = ExpressionToCodeLib.ExpressionToCode.ToCode(theComplexMethod.Body);
        //	Assert.NotEqual(full, actual);
        //}
        [Fact]
        public void TheGenericMethod_ToNameOf()
        {
            var actual = ExpressionWithName.ToNameOf(() => TheGenericMethod<int>(2));
            Assert.Equal("TheGenericMethod", actual);
        }

        [Fact]
        public void TheProperty_ToNameOf()
        {
            var actual = ExpressionWithName.ToNameOf(() => TheProperty);
            Assert.Equal("TheProperty", actual);
        }

        [Fact]
        public void TheSimpleMethod_ToNameOf()
        {
            var actual = ExpressionWithName.ToNameOf(() => TheSimpleMethod());
            Assert.Equal("TheSimpleMethod", actual);
        }

        public void TheSimpleMethod() { }
        static string TheProperty { get { return "TheValue"; } }
        // ReSharper disable once UnusedParameter.Local
        string this[int index] { get { return "TheIndexedValue"; } }
        static int StaticReturnZero() { return 0; }
        // ReSharper disable MemberCanBeMadeStatic.Local
        static string TheComplexMethod(int parameter1, string parameter2) { return "TheMethod " + parameter1 + " " + parameter2; }
        // ReSharper disable once UnusedTypeParameter
        static string TheGenericMethod<T>(int two) { return "Return value is " + two * two; }
        int ReturnZero() { return 0; }
    }
}
