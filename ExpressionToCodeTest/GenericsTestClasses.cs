using ExpressionToCodeLib.Unstable_v2_Api;
// ReSharper disable ConvertToConstant.Local
// ReSharper disable RedundantEnumerableCastCall
// ReSharper disable MemberCanBeMadeStatic.Local
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ExpressionToCodeLib;

namespace ExpressionToCodeTest {
    [TestFixture]
    public class TestGenerics {
        [Test]
        public void TypeParameters() {
            Assert.AreEqual(1337, StaticTestClass.Consume(12));
            Assert.AreEqual(42, StaticTestClass.Consume<int>(12));
            Assert.AreEqual(42, StaticTestClass.Consume('a'));
            Assert.AreEqual(42, StaticTestClass.IndirectConsume(12));

            Assert.AreEqual(
                @"() => 1337 == StaticTestClass.Consume(12)",
                ExpressionToCode.ToCode(() => 1337 == StaticTestClass.Consume(12))
                );
            Assert.AreEqual(
                @"() => 42 == StaticTestClass.Consume('a')",
                ExpressionToCode.ToCode(() => 42 == StaticTestClass.Consume('a'))
                );
            Assert.AreEqual(
                @"() => 42 == StaticTestClass.IndirectConsume(12)",
                ExpressionToCode.ToCode(() => 42 == StaticTestClass.IndirectConsume(12))
                );
            Assert.AreEqual(
                @"() => 42 == StaticTestClass.Consume<int>(12)",
                ExpressionToCode.ToCode(() => 42 == StaticTestClass.Consume<int>(12))
                ); //should not remove type parameters where this would cause ambiguity due to overloads!
        }

        [Test]
        public void TypeParameters2() {
            Assert.AreEqual(
                @"() => new[] { 1, 2, 3 }.First()",
                ExpressionToCode.ToCode(() => new[] { 1, 2, 3 }.First())
                ); //should remove type parameters where they can be inferred.

            Assert.AreEqual(
                @"() => new[] { 1, 2, 3 }.Select(x => x.ToString())",
                ExpressionToCode.ToCode(() => new[] { 1, 2, 3 }.Select(x => x.ToString()))
                ); //should remove type parameters where they can be inferred.
        }

        [Test]
        public void TypeParameters3() {
            Assert.AreEqual(
                @"() => new[] { 1, 2, 3 }.Cast<int>()",
                ExpressionToCode.ToCode(() => new[] { 1, 2, 3 }.Cast<int>())
                ); //should not remove type parameters where these cannot be inferred!
        }

        [Test]
        public void GenericConstructor() {
            Assert.AreEqual(
                @"() => new GenericClass<int>()",
                ExpressionToCode.ToCode(() => new GenericClass<int>())
                );
            Assert.AreEqual(
                @"() => new GenericClass<int>(3)",
                ExpressionToCode.ToCode(() => new GenericClass<int>(3))
                );
            Assert.AreEqual(
                @"() => new GenericSubClass<IEnumerable<int>, int>(new[] { 3 })",
                ExpressionToCode.ToCode(() => new GenericSubClass<IEnumerable<int>, int>(new[] { 3 }))
                );
        }

        [Test]
        public void MethodInGenericClass() {
            Assert.AreEqual(
                @"() => new GenericClass<int>().IsSet()",
                ExpressionToCode.ToCode(() => new GenericClass<int>().IsSet())
                );
            Assert.AreEqual(
                @"() => GenericClass<int>.GetDefault()",
                ExpressionToCode.ToCode(() => GenericClass<int>.GetDefault())
                );
        }

        [Test]
        public void GenericMethodInGenericClass() {
            var x = new GenericClass<string>("42");
            var y = new GenericClass<object>("42");
            Assert.That(x.IsSubEqual("42"));
            Assert.That(x.IsSubClass<string>());
            Assert.That(y.IsSubClass<string>());
            Assert.AreEqual(
                @"() => x.IsSubEqual(""42"")",
                ExpressionToCode.ToCode(() => x.IsSubEqual("42"))
                );
            Assert.AreEqual(
                @"() => x.IsSubClass<string>()",
                ExpressionToCode.ToCode(() => x.IsSubClass<string>())
                );
            Assert.AreEqual(
                @"() => y.IsSubClass<string>()",
                ExpressionToCode.ToCode(() => y.IsSubClass<string>())
                );
        }

        [Test]
        public void StraightforwardInference() {
            Assert.AreEqual(
                @"() => StaticTestClass.Identity(3)",
                ExpressionToCode.ToCode(() => StaticTestClass.Identity(3))
                );
        }

        [Test]
        public void CannotInferOneParam() {
            Assert.AreEqual(
                @"() => StaticTestClass.IsType<int, int>(3)",
                ExpressionToCode.ToCode(() => StaticTestClass.IsType<int, int>(3))
                );
        }

        [Test]
        public void CannotInferWithoutTParam() {
            Assert.AreEqual(
                @"() => StaticTestClass.TEqualsInt<int>(3)",
                ExpressionToCode.ToCode(() => StaticTestClass.TEqualsInt<int>(3))
                );
            Assert.AreEqual(
                @"() => StaticTestClass.TEqualsInt<string>(3)",
                ExpressionToCode.ToCode(() => StaticTestClass.TEqualsInt<string>(3))
                );
        }

        [Test]
        public void CanInferDirect() {
            Assert.AreEqual(
                @"() => StaticTestClass.TwoArgsOneGeneric(3, 3)",
                ExpressionToCode.ToCode(() => StaticTestClass.TwoArgsOneGeneric(3, 3))
                );
            Assert.AreEqual(
                @"() => StaticTestClass.TwoArgsOneGeneric(3, ""3"")",
                ExpressionToCode.ToCode(() => StaticTestClass.TwoArgsOneGeneric(3, "3"))
                );
        }

        [Test]
        public void CanInferTwoArg() {
            Assert.AreEqual(
                @"() => StaticTestClass.TwoArgsTwoGeneric(3, 3)",
                ExpressionToCode.ToCode(() => StaticTestClass.TwoArgsTwoGeneric(3, 3))
                );

            Assert.AreEqual(
                @"() => StaticTestClass.TwoArgsTwoGeneric((object)3, new object())",
                ExpressionToCode.ToCode(() => StaticTestClass.TwoArgsTwoGeneric(3, new object()))
                );

            int x = 37;
            double y = 42.0;
            Assert.AreEqual(
                @"() => StaticTestClass.TwoArgsTwoGeneric((double)x, y)",
                ExpressionToCode.ToCode(() => StaticTestClass.TwoArgsTwoGeneric(x, y))
                );
        }

        [Test, Ignore("issue 14")]
        public void CanInferIndirect() {
            Assert.That(GenericClass<int>.IsEnumerableOfType(new[] { 3, 4 }));
            Assert.That(GenericClass<int>.IsFuncOfType(() => 3));
            Assert.That(!GenericClass<int>.IsFuncOfType(() => 3.0));
            Assert.That(GenericClass<int>.IsFunc2OfType((int x) => x));

            Assert.AreEqual(
                @"() => GenericClass<int>.IsEnumerableOfType(new[] { 3, 4 })",
                ExpressionToCode.ToCode(() => GenericClass<int>.IsEnumerableOfType(new[] { 3, 4 }))
                );
            Assert.AreEqual(
                @"() => GenericClass<int>.IsFuncOfType(() => 3)",
                ExpressionToCode.ToCode(() => GenericClass<int>.IsFuncOfType(() => 3))
                );
            Assert.AreEqual(
                @"() => !GenericClass<int>.IsFuncOfType(() => 3.0)",
                ExpressionToCode.ToCode(() => !GenericClass<int>.IsFuncOfType(() => 3.0))
                );
            Assert.AreEqual(
                @"() => GenericClass<int>.IsFunc2OfType((int x) => x)",
                ExpressionToCode.ToCode(() => GenericClass<int>.IsFunc2OfType((int x) => x))
                );
        }

        [Test]
        public void GenericMethodCall_WhenSomeNotInferredTypeArguments_ShouldExplicitlySpecifyTypeArguments() {
            Assert.AreEqual(
                @"() => StaticTestClass.IsType<int, int>(3)",
                ExpressionStringify.With(explicitMethodTypeArgs: true).ToCode(() => StaticTestClass.IsType<int, int>(3))
                );
        }

        [Test]
        public void GenericMethodCall_ShouldExplicitlySpecifyTypeArguments() {
            Assert.AreEqual(
                "() => MakeMe<Cake, string>(() => new Cake())",
                ExpressionToCode.ToCode(() => MakeMe<Cake, string>(() => new Cake())));
        }

        T MakeMe<T, TNotInferredFromArgument>(Func<T> maker) { return maker(); }

        [Test]
        public void UsesBoundTypeNamesEvenInGenericMethod() {
            AssertInGenericMethodWithIntArg<int>();
        }

        void AssertInGenericMethodWithIntArg<T>() {
            //The expression no longer has any reference to the unbound argument T, so we can't generate the exactly correct code here.
            Assert.AreEqual(
                "() => new List<int>()",
                ExpressionToCode.ToCode(() => new List<T>()));
        }
    }

    internal class Cake { }

    class GenericClass<T> {
        T val;
        public GenericClass(T pVal) { val = pVal; }
        public GenericClass() { val = default(T); }
        public T Value { get { return val; } }
        public void Reset(T pVal) { val = pVal; }
        public void Reset() { val = default(T); }
        public bool IsSet() { return Equals(default(T), val); }
        public static T GetDefault() { return default(T); }
        public static bool IsEnumerableOfType<U>(IEnumerable<U> x) { return typeof(T).IsAssignableFrom(typeof(U)); }
        public static bool IsFuncOfType<U>(Func<U> x) { return typeof(T).IsAssignableFrom(typeof(U)); }
        public static bool IsFunc2OfType<U>(Func<U, U> x) { return typeof(T).IsAssignableFrom(typeof(U)); }
        public bool IsSubClass<U>() where U : T { return val is U; }
        public bool IsSubEqual<U>(U other) where U : T, IEquatable<T> { return other.Equals(val); }
    }

    class GenericSubClass<T, U> : GenericClass<T>
        where T : IEnumerable<U> {
        public GenericSubClass(T val)
            : base(val) { }

        public bool IsEmpty { get { return !Value.Any(); } }
    }

    public static class StaticTestClass {
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
