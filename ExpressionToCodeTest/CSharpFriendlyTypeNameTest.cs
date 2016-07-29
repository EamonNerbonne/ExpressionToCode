using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ExpressionToCodeLib;
using Xunit;
using Assert = Xunit.Assert;

namespace ExpressionToCodeTest
{
    public class CSharpFriendlyTypeNameTest
    {
        [Fact]
        public void SupportsBuiltInCases()
        {
            Assert.Equal("object", ObjectToCode.ToCSharpFriendlyTypeName(typeof(object)));
            Assert.Equal("string", ObjectToCode.ToCSharpFriendlyTypeName(typeof(string)));
            Assert.Equal("char", ObjectToCode.ToCSharpFriendlyTypeName(typeof(char)));
            Assert.Equal("byte", ObjectToCode.ToCSharpFriendlyTypeName(typeof(byte)));
            Assert.Equal("sbyte", ObjectToCode.ToCSharpFriendlyTypeName(typeof(sbyte)));
            Assert.Equal("short", ObjectToCode.ToCSharpFriendlyTypeName(typeof(short)));
            Assert.Equal("ushort", ObjectToCode.ToCSharpFriendlyTypeName(typeof(ushort)));
            Assert.Equal("int", ObjectToCode.ToCSharpFriendlyTypeName(typeof(int)));
            Assert.Equal("uint", ObjectToCode.ToCSharpFriendlyTypeName(typeof(uint)));
            Assert.Equal("long", ObjectToCode.ToCSharpFriendlyTypeName(typeof(long)));
            Assert.Equal("ulong", ObjectToCode.ToCSharpFriendlyTypeName(typeof(ulong)));
            Assert.Equal("void", ObjectToCode.ToCSharpFriendlyTypeName(typeof(void)));
            Assert.Equal("float", ObjectToCode.ToCSharpFriendlyTypeName(typeof(float)));
            Assert.Equal("decimal", ObjectToCode.ToCSharpFriendlyTypeName(typeof(decimal)));
        }

        [Fact]
        public void SupportsSimpleExamples()
        {
            Assert.Equal("DateTime", ObjectToCode.ToCSharpFriendlyTypeName(typeof(DateTime)));
            Assert.Equal("Regex", ObjectToCode.ToCSharpFriendlyTypeName(typeof(Regex)));
            Assert.Equal("ExpressionToCode", ObjectToCode.ToCSharpFriendlyTypeName(typeof(ExpressionToCode)));
        }

        [Fact]
        public void IntArray()
        {
            Assert.Equal("int[]", ObjectToCode.ToCSharpFriendlyTypeName(typeof(int[])));
        }

        [Fact]
        public void NullableValueType()
        {
            Assert.Equal("ConsoleKey?", ObjectToCode.ToCSharpFriendlyTypeName(typeof(ConsoleKey?)));
        }

        [Fact]
        public void GenericList()
        {
            Assert.Equal("List<DateTime>", ObjectToCode.ToCSharpFriendlyTypeName(typeof(List<DateTime>)));
        }

        [Fact]
        public void MultiDimArray()
        {
            Assert.Equal("string[,,]", ObjectToCode.ToCSharpFriendlyTypeName(typeof(string[,,])));
        }

        [Fact] //Has always been broken
        public void MultiDimOfSingleDimArray()
        {
            Assert.Equal("object[,][]", ObjectToCode.ToCSharpFriendlyTypeName(typeof(object[,][])));
        }

        [Fact] //Has always been broken
        public void SingleDimOfMultiDimArray()
        {
            Assert.Equal("object[][,]", ObjectToCode.ToCSharpFriendlyTypeName(typeof(object[][,])));
        }

        [Fact] //Has always been broken
        public void ConstructedSingleDimOfMultiDimArray()
        {
            // ReSharper disable once SuggestUseVarKeywordEvident
            // ReSharper disable once RedundantArrayCreationExpression
            object[][,] v = new[] { new object[2, 3] };

            Assert.Equal("object[][,]", ObjectToCode.ToCSharpFriendlyTypeName(v.GetType()));
        }

        [Fact]
        public void ArrayGenericsMessyMix()
        {
            Assert.Equal("List<Tuple<int[], string[,]>[][]>[]", ObjectToCode.ToCSharpFriendlyTypeName(typeof(List<Tuple<int[], string[,]>[][]>[])));
        }

        [Fact]
        public void NestedClasses()
        {
            Assert.Equal("Outer<string, int>.Nested<DateTime>", ObjectToCode.ToCSharpFriendlyTypeName(typeof(Outer<string, int>.Nested<DateTime>)));
        }

        [Fact]
        public void NestedNonGenericInGenericClasses()
        {
            Assert.Equal("Outer<string, int>.Nested2", ObjectToCode.ToCSharpFriendlyTypeName(typeof(Outer<string, int>.Nested2)));
        }

        [Fact]
        public void NestedGenericInNonGenericClasses()
        {
            Assert.Equal("Outer2.Nested3<Action>", ObjectToCode.ToCSharpFriendlyTypeName(typeof(Outer2.Nested3<Action>)));
        }

        [Fact]
        public void RussianDolls()
        {
            Assert.Equal("Tuple<List<int>, Tuple<List<string>>>", ObjectToCode.ToCSharpFriendlyTypeName(typeof(Tuple<List<int>, Tuple<List<string>>>)));
        }

        [Fact]
        public void GenericArgumentTypes()
        {
            Assert.Equal("Func<Z>", ObjectToCode.ToCSharpFriendlyTypeName(typeof(Outer<,>.Nested<>).GetMethod("Method").GetParameters()[0].ParameterType));
        }

        [Fact]
        public void UnboundNested()
        {
            Assert.Equal("Outer<X, Y>.Nested<Z>", ObjectToCode.ToCSharpFriendlyTypeName(typeof(Outer<,>.Nested<>)));
        }

        [Fact]
        public void UnboundGenericList()
        {
            Assert.Equal("List<T>", ObjectToCode.ToCSharpFriendlyTypeName(typeof(List<>)));
        }

        [Fact]
        public void UnboundGenericListInTypeof()
        {
            Assert.Equal("() => typeof(List<>)", ExpressionToCode.ToCode(() => typeof(List<>)));
        }

        [Fact]
        public void UnboundGenericNullableInTypeof()
        {
            Assert.Equal("() => typeof(Nullable<>)", ExpressionToCode.ToCode(() => typeof(Nullable<>)));
        }

        [Fact()]
        public void UnboundNestedInTypeof()
        {
            Assert.Equal("() => typeof(Outer<,>.Nested<>)", ExpressionToCode.ToCode(() => typeof(Outer<,>.Nested<>)));
        }
    }

    public class Outer<X, Y>
    {
        public class Nested<Z>
        {
            public void Method(Func<Z> arg) { }
        }

        public class Nested2 { }
    }

    public class Outer2
    {
        public class Nested3<Z>
        {
            public void Method(Func<Z> arg) { }
        }
    }
}
