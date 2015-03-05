using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using ExpressionToCodeLib;
using NUnit.Framework;

namespace ExpressionToCodeTest
{
    [TestFixture]
    public class CSharpFriendlyTypeNameTest
    {
        [Test]
        public void SupportsBuiltInCases()
        {
            Assert.AreEqual("object", ObjectToCode.GetCSharpFriendlyTypeName(typeof(object)));
            Assert.AreEqual("string", ObjectToCode.GetCSharpFriendlyTypeName(typeof(string)));
            Assert.AreEqual("char", ObjectToCode.GetCSharpFriendlyTypeName(typeof(char)));
            Assert.AreEqual("byte", ObjectToCode.GetCSharpFriendlyTypeName(typeof(byte)));
            Assert.AreEqual("sbyte", ObjectToCode.GetCSharpFriendlyTypeName(typeof(sbyte)));
            Assert.AreEqual("short", ObjectToCode.GetCSharpFriendlyTypeName(typeof(short)));
            Assert.AreEqual("ushort", ObjectToCode.GetCSharpFriendlyTypeName(typeof(ushort)));
            Assert.AreEqual("int", ObjectToCode.GetCSharpFriendlyTypeName(typeof(int)));
            Assert.AreEqual("uint", ObjectToCode.GetCSharpFriendlyTypeName(typeof(uint)));
            Assert.AreEqual("long", ObjectToCode.GetCSharpFriendlyTypeName(typeof(long)));
            Assert.AreEqual("ulong", ObjectToCode.GetCSharpFriendlyTypeName(typeof(ulong)));
            Assert.AreEqual("void", ObjectToCode.GetCSharpFriendlyTypeName(typeof(void)));
        }

        [Test]
        public void SupportsSimpleExamples()
        {
            Assert.AreEqual("DateTime", ObjectToCode.GetCSharpFriendlyTypeName(typeof(DateTime)));
            Assert.AreEqual("Regex", ObjectToCode.GetCSharpFriendlyTypeName(typeof(Regex)));
            Assert.AreEqual("ExpressionToCode", ObjectToCode.GetCSharpFriendlyTypeName(typeof(ExpressionToCode)));
        }

        [Test]
        public void IntArray()
        {
            Assert.AreEqual("int[]", ObjectToCode.GetCSharpFriendlyTypeName(typeof(int[])));
        }

        [Test]
        public void GenericList()
        {
            Assert.AreEqual("List<DateTime>", ObjectToCode.GetCSharpFriendlyTypeName(typeof(List<DateTime>)));
        }

        [Test]
        public void MultiDimArray()
        {
            Assert.AreEqual("string[,,]", ObjectToCode.GetCSharpFriendlyTypeName(typeof(string[,,])));
        }

        [Test]//Has always been broken
        public void MultiDimOfSingleDimArray()
        {
            Assert.AreEqual("object[,][]", ObjectToCode.GetCSharpFriendlyTypeName(typeof(object[,][])));
        }

        [Test]//Has always been broken
        public void SingleDimOfMultiDimArray()
        {
            Assert.AreEqual("object[][,]", ObjectToCode.GetCSharpFriendlyTypeName(typeof(object[][,])));
        }

        [Test]//Has always been broken
        public void ConstructedSingleDimOfMultiDimArray()
        {
            // ReSharper disable once SuggestUseVarKeywordEvident
            // ReSharper disable once RedundantArrayCreationExpression
            object[][,] v = new[] { new object[2, 3] };

            Assert.AreEqual("object[][,]", ObjectToCode.GetCSharpFriendlyTypeName(v.GetType()));
        }

        [Test]
        public void ArrayGenericsMessyMix()
        {
            Assert.AreEqual("List<Tuple<int[], string[,]>[][]>[]", ObjectToCode.GetCSharpFriendlyTypeName(typeof(List<Tuple<int[], string[,]>[][]>[])));
        }

        [Test]
        public void NestedClasses()
        {
            Assert.AreEqual("Outer<string, int>.Nested<DateTime>", ObjectToCode.GetCSharpFriendlyTypeName(typeof(Outer<string, int>.Nested<DateTime>)));
        }

        [Test]
        public void RussianDolls()
        {
            Assert.AreEqual("Tuple<List<int>, Tuple<List<string>>>", ObjectToCode.GetCSharpFriendlyTypeName(typeof(Tuple<List<int>, Tuple<List<string>>>)));
        }

        [Test] //TODO:Regression!
        public void UnboundNested()
        {
            Assert.AreEqual("Outer<X,Y>.Nested<Z>", ObjectToCode.GetCSharpFriendlyTypeName(typeof(Outer<,>.Nested<>)));
        }

        [Test] //TODO:Regression!
        public void UnboundGenericList()
        {
            Assert.AreEqual("List<T>", ObjectToCode.GetCSharpFriendlyTypeName(typeof(List<>)));
        }

        [Test] //TODO: this never worked, but it shouldn't crash
        public void UnboundGenericListInTypeof()
        {
            Assert.AreEqual("() => typeof(List<>)", ExpressionToCode.ToCode(() => typeof(List<>)));
        }

        [Test] //TODO: this never worked, but it shouldn't crash
        public void UnboundNestedInTypeof()
        {
            Assert.AreEqual("() => typeof(Outer<,>.Nested<>)", ExpressionToCode.ToCode(() => typeof(Outer<,>.Nested<>)));
        }
    }

    public class Outer<X, Y>
    {
        public class Nested<Z> { }
    }
}
