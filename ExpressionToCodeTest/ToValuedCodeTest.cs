using ExpressionToCodeTest.Unstable_v2_Api;
// ReSharper disable RedundantEnumerableCastCall
// ReSharper disable RedundantNameQualifier
// ReSharper disable ConvertToConstant.Local
// ReSharper disable RedundantLogicalConditionalExpressionOperand
// ReSharper disable RedundantCast
// ReSharper disable ConstantNullCoalescingCondition
// ReSharper disable EqualExpressionComparison
// ReSharper disable RedundantToStringCall

#pragma warning disable 1720
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Xml;
using NUnit.Framework;
using ExpressionToCodeLib;

namespace ExpressionToCodeTest
{
    [TestFixture]
    public class ToValuedCodeTest
    {
   
        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ToValuedCode_ofNull_fails()
        {
            ExpressionToCodeTest obj = null;
            var actual = ExpressionWithValue.ToValuedCode(() => obj.TheProperty);
        }


        [Test]
        public void ArrayLength()
        {
            var arr = new int[] { 1 };
            var actual = ExpressionWithValue.ToValuedCode(() => arr.Length);
            Assert.AreEqual("arr.Length = 1",actual);
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
            Expression<Func<string>> theMethod = () => TheMethod(1, "2");  //can be invoked later
            var actual = ExpressionWithValue.ToValuedCode(theMethod);
            Assert.AreEqual("TheMethod(1, \"2\") = TheMethod 1 2", actual);
        }

        [Test]
        public void TheGenericMethod()
        {
            Expression<Func<string>> theGenericMethod = () => TheGenericMethod<int>(2);
            var actual = ExpressionWithValue.ToValuedCode(theGenericMethod);
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

        public string TheProperty
        {
            get
            {
                return "TheValue";
            }
        }

        public string this[int index]
        {
            get
            {
                return "TheIndexedValue";
            }
        }

        public string TheMethod(int parameter1, string parameter2)
        {
            return "TheMethod " + parameter1 + " " + parameter2;
        }

        public string TheGenericMethod<T>(int two)
        {
            return "Return value is " + two * two;
        }

        public int ReturnZero()
        {
            return 0;
        }

        public static int StaticReturnZero()
        {
            return 0;
        }
    }
}
