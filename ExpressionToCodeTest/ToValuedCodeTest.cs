using ExpressionToCodeTest.Unstable_v2_Api;
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ExpressionToCodeTest
{
    [TestFixture]
    public class ToValuedCodeTest
    {
        [Test]
        public void ToValuedCode_ofNull_fails()
        {
            ExpressionToCodeTest obj = null;
            Assert.Catch<InvalidOperationException>(
                () =>
                    ExpressionWithValue.ToValuedCode(() => obj.TheProperty));
        }

        [Test]
        public void ArrayLength()
        {
            var arr = new[] { 1 };
            var actual = ExpressionWithValue.ToValuedCode(() => arr.Length);
            Assert.AreEqual("arr.Length = 1", actual);
        }

        [Test]
        public void ThePropertyAccess()
        {
            var actual = ExpressionWithValue.ToValuedCode(() => TheProperty);
            Assert.AreEqual("TheProperty = TheValue", actual);
        }

        [Test]
        public void TheStringVariable()
        {
            var theVariable = "theValue";
            var actual = ExpressionWithValue.ToValuedCode(() => theVariable);
            Assert.AreEqual("theVariable = theValue", actual);
        }

        [Test]
        public void TheMethod()
        {
            var actual = ExpressionWithValue.ToValuedCode(() => TheMethod(1, "2"));
            Assert.AreEqual("TheMethod(1, \"2\") = TheMethod 1 2", actual);
        }

        [Test]
        public void TheGenericMethod()
        {
            var actual = ExpressionWithValue.ToValuedCode(() => TheGenericMethod<int>(2));
            Assert.AreEqual("TheGenericMethod<Int32>(2) = Return value is 4", actual);
        }

        [Test]
        public void ThisIndexedProperty()
        {
            var actual = ExpressionWithValue.ToValuedCode(() => this[1]);
            Assert.AreEqual("ToValuedCodeTest[1] = TheIndexedValue", actual);
        }

        [Test]
        public void ThisMethodCall()
        {
            var code = ExpressionWithValue.ToValuedCode(() => ReturnZero());

            Assert.AreEqual("ReturnZero() = 0", code);
        }

        [Test]
        public void ThisStaticMethodCall()
        {
            var code = ExpressionWithValue.ToValuedCode(() => StaticReturnZero());

            Assert.AreEqual("ToValuedCodeTest.StaticReturnZero() = 0", code);
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
