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
            Assert.Equal("object", ObjectToCode.GetCSharpFriendlyTypeName(typeof(object)));
            Assert.Equal("string", ObjectToCode.GetCSharpFriendlyTypeName(typeof(string)));
            Assert.Equal("char", ObjectToCode.GetCSharpFriendlyTypeName(typeof(char)));
            Assert.Equal("byte", ObjectToCode.GetCSharpFriendlyTypeName(typeof(byte)));
            Assert.Equal("sbyte", ObjectToCode.GetCSharpFriendlyTypeName(typeof(sbyte)));
            Assert.Equal("short", ObjectToCode.GetCSharpFriendlyTypeName(typeof(short)));
            Assert.Equal("ushort", ObjectToCode.GetCSharpFriendlyTypeName(typeof(ushort)));
            Assert.Equal("int", ObjectToCode.GetCSharpFriendlyTypeName(typeof(int)));
            Assert.Equal("uint", ObjectToCode.GetCSharpFriendlyTypeName(typeof(uint)));
            Assert.Equal("long", ObjectToCode.GetCSharpFriendlyTypeName(typeof(long)));
            Assert.Equal("ulong", ObjectToCode.GetCSharpFriendlyTypeName(typeof(ulong)));
            Assert.Equal("void", ObjectToCode.GetCSharpFriendlyTypeName(typeof(void)));
            Assert.Equal("float", ObjectToCode.GetCSharpFriendlyTypeName(typeof(float)));
            Assert.Equal("decimal", ObjectToCode.GetCSharpFriendlyTypeName(typeof(decimal)));
        }

        [Fact]
        public void SupportsSimpleExamples()
        {
            Assert.Equal("DateTime", ObjectToCode.GetCSharpFriendlyTypeName(typeof(DateTime)));
            Assert.Equal("Regex", ObjectToCode.GetCSharpFriendlyTypeName(typeof(Regex)));
            Assert.Equal("ExpressionToCode", ObjectToCode.GetCSharpFriendlyTypeName(typeof(ExpressionToCode)));
        }

        [Fact]
        public void IntArray()
        {
            Assert.Equal("int[]", ObjectToCode.GetCSharpFriendlyTypeName(typeof(int[])));
        }

        [Fact]
        public void NullableValueType()
        {
            Assert.Equal("ConsoleKey?", ObjectToCode.GetCSharpFriendlyTypeName(typeof(ConsoleKey?)));
        }

        [Fact]
        public void GenericList()
        {
            Assert.Equal("List<DateTime>", ObjectToCode.GetCSharpFriendlyTypeName(typeof(List<DateTime>)));
        }

        [Fact]
        public void MultiDimArray()
        {
            Assert.Equal("string[,,]", ObjectToCode.GetCSharpFriendlyTypeName(typeof(string[,,])));
        }

        [Fact] //Has always been broken
        public void MultiDimOfSingleDimArray()
        {
            Assert.Equal("object[,][]", ObjectToCode.GetCSharpFriendlyTypeName(typeof(object[,][])));
        }

        [Fact] //Has always been broken
        public void SingleDimOfMultiDimArray()
        {
            Assert.Equal("object[][,]", ObjectToCode.GetCSharpFriendlyTypeName(typeof(object[][,])));
        }

        [Fact] //Has always been broken
        public void ConstructedSingleDimOfMultiDimArray()
        {
            // ReSharper disable once SuggestUseVarKeywordEvident
            // ReSharper disable once RedundantArrayCreationExpression
            object[][,] v = new[] { new object[2, 3] };

            Assert.Equal("object[][,]", ObjectToCode.GetCSharpFriendlyTypeName(v.GetType()));
        }

        [Fact]
        public void ArrayGenericsMessyMix()
        {
            Assert.Equal("List<Tuple<int[], string[,]>[][]>[]", ObjectToCode.GetCSharpFriendlyTypeName(typeof(List<Tuple<int[], string[,]>[][]>[])));
        }

        [Fact]
        public void NestedClasses()
        {
            Assert.Equal("Outer<string, int>.Nested<DateTime>", ObjectToCode.GetCSharpFriendlyTypeName(typeof(Outer<string, int>.Nested<DateTime>)));
        }

        [Fact]
        public void NestedNonGenericInGenericClasses()
        {
            Assert.Equal("Outer<string, int>.Nested2", ObjectToCode.GetCSharpFriendlyTypeName(typeof(Outer<string, int>.Nested2)));
        }

        [Fact]
        public void NestedGenericInNonGenericClasses()
        {
            Assert.Equal("Outer2.Nested3<Action>", ObjectToCode.GetCSharpFriendlyTypeName(typeof(Outer2.Nested3<Action>)));
        }

        [Fact]
        public void RussianDolls()
        {
            Assert.Equal("Tuple<List<int>, Tuple<List<string>>>", ObjectToCode.GetCSharpFriendlyTypeName(typeof(Tuple<List<int>, Tuple<List<string>>>)));
        }

        [Fact]
        public void GenericArgumentTypes()
        {
            Assert.Equal("Func<Z>", ObjectToCode.GetCSharpFriendlyTypeName(typeof(Outer<,>.Nested<>).GetMethod("Method").GetParameters()[0].ParameterType));
        }

        [Fact]
        public void UnboundNested()
        {
            Assert.Equal("Outer<X, Y>.Nested<Z>", ObjectToCode.GetCSharpFriendlyTypeName(typeof(Outer<,>.Nested<>)));
        }

        [Fact]
        public void UnboundGenericList()
        {
            Assert.Equal("List<T>", ObjectToCode.GetCSharpFriendlyTypeName(typeof(List<>)));
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
