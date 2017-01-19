using System.Reflection;
using Xunit;
using Assert = Xunit.Assert;
// ReSharper disable ConvertToConstant.Local
// ReSharper disable RedundantEnumerableCastCall
// ReSharper disable MemberCanBeMadeStatic.Local
using System;
using System.Collections.Generic;
using System.Linq;
using ExpressionToCodeLib;

namespace ExpressionToCodeTest
{
    public class TestGenerics
    {
        [Fact]
        public void TypeParameters()
        {
            Assert.Equal(1337, StaticTestClass.Consume(12));
            Assert.Equal(42, StaticTestClass.Consume<int>(12));
            Assert.Equal(42, StaticTestClass.Consume('a'));
            Assert.Equal(42, StaticTestClass.IndirectConsume(12));

            Assert.Equal(
                @"() => 1337 == StaticTestClass.Consume(12)",
                ExpressionToCode.ToCode(() => 1337 == StaticTestClass.Consume(12))
                );
            Assert.Equal(
                @"() => 42 == StaticTestClass.Consume('a')",
                ExpressionToCode.ToCode(() => 42 == StaticTestClass.Consume('a'))
                );
            Assert.Equal(
                @"() => 42 == StaticTestClass.IndirectConsume(12)",
                ExpressionToCode.ToCode(() => 42 == StaticTestClass.IndirectConsume(12))
                );
            Assert.Equal(
                @"() => 42 == StaticTestClass.Consume<int>(12)",
                ExpressionToCode.ToCode(() => 42 == StaticTestClass.Consume<int>(12))
                ); //should not remove type parameters where this would cause ambiguity due to overloads!
        }

        [Fact]
        public void TypeParameters2()
        {
            Assert.Equal(
                @"() => new[] { 1, 2, 3 }.First()",
                ExpressionToCode.ToCode(() => new[] { 1, 2, 3 }.First())
                ); //should remove type parameters where they can be inferred.

            Assert.Equal(
                @"() => new[] { 1, 2, 3 }.Select(x => x.ToString())",
                ExpressionToCode.ToCode(() => new[] { 1, 2, 3 }.Select(x => x.ToString()))
                ); //should remove type parameters where they can be inferred.
        }

        [Fact]
        public void TypeParameters3()
        {
            Assert.Equal(
                @"() => new[] { 1, 2, 3 }.Cast<int>()",
                ExpressionToCode.ToCode(() => new[] { 1, 2, 3 }.Cast<int>())
                ); //should not remove type parameters where these cannot be inferred!
        }

        [Fact]
        public void GenericConstructor()
        {
            Assert.Equal(
                @"() => new GenericClass<int>()",
                ExpressionToCode.ToCode(() => new GenericClass<int>())
                );
            Assert.Equal(
                @"() => new GenericClass<int>(3)",
                ExpressionToCode.ToCode(() => new GenericClass<int>(3))
                );
            Assert.Equal(
                @"() => new GenericSubClass<IEnumerable<int>, int>(new[] { 3 })",
                ExpressionToCode.ToCode(() => new GenericSubClass<IEnumerable<int>, int>(new[] { 3 }))
                );
        }

        [Fact]
        public void MethodInGenericClass()
        {
            Assert.Equal(
                @"() => new GenericClass<int>().IsSet()",
                ExpressionToCode.ToCode(() => new GenericClass<int>().IsSet())
                );
            Assert.Equal(
                @"() => GenericClass<int>.GetDefault()",
                ExpressionToCode.ToCode(() => GenericClass<int>.GetDefault())
                );
        }

        [Fact]
        public void GenericMethodInGenericClass()
        {
            var x = new GenericClass<string>("42");
            var y = new GenericClass<object>("42");
            Assert.True(x.IsSubEqual("42"));
            Assert.True(x.IsSubClass<string>());
            Assert.True(y.IsSubClass<string>());
            Assert.Equal(
                @"() => x.IsSubEqual(""42"")",
                ExpressionToCode.ToCode(() => x.IsSubEqual("42"))
                );
            Assert.Equal(
                @"() => x.IsSubClass<string>()",
                ExpressionToCode.ToCode(() => x.IsSubClass<string>())
                );
            Assert.Equal(
                @"() => y.IsSubClass<string>()",
                ExpressionToCode.ToCode(() => y.IsSubClass<string>())
                );
        }

        [Fact]
        public void StraightforwardInference()
        {
            Assert.Equal(
                @"() => StaticTestClass.Identity(3)",
                ExpressionToCode.ToCode(() => StaticTestClass.Identity(3))
                );
        }

        [Fact]
        public void CannotInferOneParam()
        {
            Assert.Equal(
                @"() => StaticTestClass.IsType<int, int>(3)",
                ExpressionToCode.ToCode(() => StaticTestClass.IsType<int, int>(3))
                );
        }

        [Fact]
        public void CannotInferWithoutTParam()
        {
            Assert.Equal(
                @"() => StaticTestClass.TEqualsInt<int>(3)",
                ExpressionToCode.ToCode(() => StaticTestClass.TEqualsInt<int>(3))
                );
            Assert.Equal(
                @"() => StaticTestClass.TEqualsInt<string>(3)",
                ExpressionToCode.ToCode(() => StaticTestClass.TEqualsInt<string>(3))
                );
        }

        [Fact]
        public void CanInferDirect()
        {
            Assert.Equal(
                @"() => StaticTestClass.TwoArgsOneGeneric(3, 3)",
                ExpressionToCode.ToCode(() => StaticTestClass.TwoArgsOneGeneric(3, 3))
                );
            Assert.Equal(
                @"() => StaticTestClass.TwoArgsOneGeneric(3, ""3"")",
                ExpressionToCode.ToCode(() => StaticTestClass.TwoArgsOneGeneric(3, "3"))
                );
        }

        [Fact]
        public void CanInferTwoArg()
        {
            Assert.Equal(
                @"() => StaticTestClass.TwoArgsTwoGeneric(3, 3)",
                ExpressionToCode.ToCode(() => StaticTestClass.TwoArgsTwoGeneric(3, 3))
                );

            Assert.Equal(
                @"() => StaticTestClass.TwoArgsTwoGeneric((object)3, new object())",
                ExpressionToCode.ToCode(() => StaticTestClass.TwoArgsTwoGeneric(3, new object()))
                );

            int x = 37;
            double y = 42.0;
            Assert.Equal(
                @"() => StaticTestClass.TwoArgsTwoGeneric((double)x, y)",
                ExpressionToCode.ToCode(() => StaticTestClass.TwoArgsTwoGeneric(x, y))
                );
        }

        [Fact(Skip = "issue 14")]
        public void CanInferIndirect()
        {
            Assert.True(GenericClass<int>.IsEnumerableOfType(new[] { 3, 4 }));
            Assert.True(GenericClass<int>.IsFuncOfType(() => 3));
            Assert.True(!GenericClass<int>.IsFuncOfType(() => 3.0));
            Assert.True(GenericClass<int>.IsFunc2OfType((int x) => x));

            Assert.Equal(
                @"() => GenericClass<int>.IsEnumerableOfType(new[] { 3, 4 })",
                ExpressionToCode.ToCode(() => GenericClass<int>.IsEnumerableOfType(new[] { 3, 4 }))
                );
            Assert.Equal(
                @"() => GenericClass<int>.IsFuncOfType(() => 3)",
                ExpressionToCode.ToCode(() => GenericClass<int>.IsFuncOfType(() => 3))
                );
            Assert.Equal(
                @"() => !GenericClass<int>.IsFuncOfType(() => 3.0)",
                ExpressionToCode.ToCode(() => !GenericClass<int>.IsFuncOfType(() => 3.0))
                );
            Assert.Equal(
                @"() => GenericClass<int>.IsFunc2OfType((int x) => x)",
                ExpressionToCode.ToCode(() => GenericClass<int>.IsFunc2OfType((int x) => x))
                );
        }

        [Fact]
        public void GenericMethodCall_WhenSomeNotInferredTypeArguments_ShouldExplicitlySpecifyTypeArguments()
        {
            Assert.Equal(
                @"() => StaticTestClass.IsType<int, int>(3)",
                ExpressionToCodeConfiguration.DefaultConfiguration.WithAlwaysUseExplicitTypeArguments(true).WithObjectStringifier(false ? ObjectStringify.WithFullTypeNames : ObjectStringify.Default).GetExpressionToCode().ToCode(() => StaticTestClass.IsType<int, int>(3))
                );
        }

        [Fact]
        public void GenericMethodCall_ShouldExplicitlySpecifyTypeArguments()
        {
            Assert.Equal(
                "() => MakeMe<Cake, string>(() => new Cake())",
                ExpressionToCode.ToCode(() => MakeMe<Cake, string>(() => new Cake())));
        }

        T MakeMe<T, TNotInferredFromArgument>(Func<T> maker) { return maker(); }

        [Fact]
        public void UsesBoundTypeNamesEvenInGenericMethod() { AssertInGenericMethodWithIntArg<int>(); }

        void AssertInGenericMethodWithIntArg<T>()
        {
            //The expression no longer has any reference to the unbound argument T, so we can't generate the exactly correct code here.
            Assert.Equal(
                "() => new List<int>()",
                ExpressionToCode.ToCode(() => new List<T>()));
        }
    }

    internal class Cake { }

    internal class GenericClass<T>
    {
        T val;
        public GenericClass(T pVal) { val = pVal; }
        public GenericClass() { val = default(T); }
        public T Value { get { return val; } }
        public void Reset(T pVal) { val = pVal; }
        public void Reset() { val = default(T); }
        public bool IsSet() { return Equals(default(T), val); }
        public static T GetDefault() { return default(T); }
        public static bool IsEnumerableOfType<U>(IEnumerable<U> x) { return typeof(T).GetTypeInfo().IsAssignableFrom(typeof(U)); }
        public static bool IsFuncOfType<U>(Func<U> x) { return typeof(T).IsAssignableFrom(typeof(U)); }
        public static bool IsFunc2OfType<U>(Func<U, U> x) { return typeof(T).IsAssignableFrom(typeof(U)); }
        public bool IsSubClass<U>() where U : T { return val is U; }
        public bool IsSubEqual<U>(U other) where U : T, IEquatable<T> { return other.Equals(val); }
    }

    internal class GenericSubClass<T, U> : GenericClass<T>
        where T : IEnumerable<U>
    {
        public GenericSubClass(T val)
            : base(val) { }

        public bool IsEmpty { get { return !Value.Any(); } }
    }

    public static class StaticTestClass
    {
        public static T Identity<T>(T val) { return val; }
        public static bool IsType<T, U>(T val) { return val is U; }
        public static bool TEqualsInt<T>(int val) { return val is T; }
        public static bool TwoArgsOneGeneric<T>(int val, T other) { return val.Equals(other); }
        public static bool TwoArgsTwoGeneric<T>(T val, T other) { return val.Equals(other); }
        public static int Consume<T>(T val) { return 42; }
        public static int Consume(int val) { return 1337; }
        public static int IndirectConsume<T>(T val) { return Consume(val); }
    }
}
