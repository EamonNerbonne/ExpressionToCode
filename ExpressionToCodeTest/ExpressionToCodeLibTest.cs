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

namespace ExpressionToCodeTest {
	[TestFixture]
    public class ExpressionToCodeTest {
        [Test]
        public void AddOperator() {
            int x = 0;
            Assert.AreEqual(
                @"() => 1 + x + 2 == 4",
                ExpressionToCode.ToCode(() => 1 + x + 2 == 4));
        }

        [Test]
        public void AnonymousClasses() {
            Assert.AreEqual(
                @"() => new { X = 3, A = ""a"" } == new { X = 3, A = ""a"" }",
                ExpressionToCode.ToCode(() => new { X = 3, A = "a" } == new { X = 3, A = "a" }));
        }

        [Test]
        public void ArrayIndex() {
            Assert.AreEqual(
                @"() => new[] { 3, 4, 5 }[0 + (int)(DateTime.Now.Ticks % 3)] == 3",
                ExpressionToCode.ToCode(() => new[] { 3, 4, 5 }[0 + (int)(DateTime.Now.Ticks % 3)] == 3));
        }

        [Test]
        public void ArrayLengthAndDoubles() {
            Assert.AreEqual(
                @"() => new[] { 1.0, 2.01, 3.5 }.Concat(new[] { 1.0, 2.0 }).ToArray().Length == 0",
                ExpressionToCode.ToCode(() => new[] { 1.0, 2.01, 3.5 }.Concat(new[] { 1.0, 2.0 }).ToArray().Length == 0));
        }

        [Test]
        public void AsOperator() {
            Assert.AreEqual(
                @"() => new object() as string == null",
                ExpressionToCode.ToCode(() => new object() as string == null));
        }

        [Test]
        public void ComplexGenericName() {
            Assert.AreEqual(
                @"() => ((Func<int, bool>)(x => x > 0))(0)",
                ExpressionToCode.ToCode(() => ((Func<int, bool>)(x => x > 0))(0)));
        }

        [Test]
        public void DefaultValue() {
            Assert.AreEqual(
                @"() => new TimeSpan(1, 2, 3) == default(TimeSpan)",
                ExpressionToCode.ToCode(() => new TimeSpan(1, 2, 3) == default(TimeSpan)));
        }

        [Test]
        public void IndexerAccess() {
            var dict = Enumerable.Range(1, 20).ToDictionary(n => n.ToString());
            Assert.AreEqual(
                @"() => dict[""3""] == 3",
                ExpressionToCode.ToCode(() => dict["3"] == 3));
        }

        [Test]
        public void IsOperator() {
            Assert.AreEqual(
                @"() => new object() is string",
                ExpressionToCode.ToCode(() => new object() is string));
        }

        [Test]
        public void ArrayOfFuncInitializer() {
            Assert.AreEqual(
                @"() => new Func<int>[] { () => 1, () => 2 }",
                ExpressionToCode.ToCode(() => new Func<int>[] { () => 1, () => 2 }));
        }

        [Test]
        public void ListInitializer() {
            Assert.AreEqual(
                @"() => new Dictionary<int, int> { { 1, 1 }, { 2, 2 }, { 3, 4 } }.Count == 3",
                ExpressionToCode.ToCode(() => new Dictionary<int, int> { { 1, 1 }, { 2, 2 }, { 3, 4 } }.Count == 3));
        }

        [Test]
        public void ListInitializer2() {
            Assert.AreEqual(
                @"() => new List<int>(50) { 1, 2, 3 }.Count == 3",
                ExpressionToCode.ToCode(() => new List<int>(50) { 1, 2, 3 }.Count == 3));
        }

        [Test]
        public void ListInitializer3() {
            Assert.AreEqual(
                @"() => new List<int> { 1, 2, 3 }.Count == 3",
                ExpressionToCode.ToCode(() => new List<int> { 1, 2, 3 }.Count == 3));
        }

        [Test]
        public void LiteralCharAndProperty() {
            Assert.AreEqual(
                @"() => new string(' ', 3).Length == 1",
                ExpressionToCode.ToCode(() => new string(' ', 3).Length == 1));
        }

        [Test]
        public void MembersBuiltin() {
            Assert.AreEqual(
                @"() => 1.23m.ToString()",
                ExpressionToCode.ToCode(() => 1.23m.ToString()));
            Assert.AreEqual(
                @"() => AttributeTargets.All.HasFlag((Enum)AttributeTargets.Assembly)",
                ExpressionToCode.ToCode(() => AttributeTargets.All.HasFlag((Enum)AttributeTargets.Assembly)));
            Assert.AreEqual(
                @"() => ""abc"".Length == 3",
                ExpressionToCode.ToCode(() => "abc".Length == 3));
            Assert.AreEqual(
                @"() => 'a'.CompareTo('b') < 0",
                ExpressionToCode.ToCode(() => 'a'.CompareTo('b') < 0));
        }

        [Test]
        public void MembersDefault() {
            Assert.AreEqual(
                @"() => default(DateTime).Ticks == 0",
                ExpressionToCode.ToCode(() => default(DateTime).Ticks == 0));
            Assert.AreEqual(
                @"() => default(int[]).Length == 0",
                ExpressionToCode.ToCode(() => default(int[]).Length == 0));
            Assert.AreEqual(
                @"() => default(Type).IsLayoutSequential",
                ExpressionToCode.ToCode(() => default(Type).IsLayoutSequential));
            Assert.AreEqual(
                @"() => default(List<int>).Count",
                ExpressionToCode.ToCode(() => default(List<int>).Count));
            Assert.AreEqual(
                @"() => default(int[]).Clone() == null",
                ExpressionToCode.ToCode(() => default(int[]).Clone() == null));
            Assert.AreEqual(
                @"() => default(Type).IsInstanceOfType(new object())",
                ExpressionToCode.ToCode(() => default(Type).IsInstanceOfType(new object())));
            Assert.AreEqual(
                @"() => default(List<int>).AsReadOnly()",
                ExpressionToCode.ToCode(() => default(List<int>).AsReadOnly()));
        }

        [Test]
        public void MembersThis() {
            new ClassA().DoAssert();
        }

        [Test]
        public void MethodGroupAsExtensionMethod() {
            Assert.AreEqual(
                "() => (Func<bool>)new[] { 2000, 2004, 2008, 2012 }.Any"
                ,
                ExpressionToCode.ToCode(() => (Func<bool>)new[] { 2000, 2004, 2008, 2012 }.Any));
        }

        [Test]
        public void MethodGroupConstant() {
            Assert.AreEqual(
                @"() => Array.TrueForAll(new[] { 2000, 2004, 2008, 2012 }, (Predicate<int>)DateTime.IsLeapYear)",
                ExpressionToCode.ToCode(() => Array.TrueForAll(new[] { 2000, 2004, 2008, 2012 }, DateTime.IsLeapYear)));

            HashSet<int> set = new HashSet<int>();
            Assert.AreEqual(
                @"() => new[] { 2000, 2004, 2008, 2012 }.All((Func<int, bool>)set.Add)",
                ExpressionToCode.ToCode(() => new[] { 2000, 2004, 2008, 2012 }.All(set.Add)));

            Func<Func<object, object, bool>, bool> sink = f => f(null, null);
            Assert.AreEqual(
                @"() => sink((Func<object, object, bool>)object.Equals)",
                ExpressionToCode.ToCode(() => sink(int.Equals)));
        }

        [Test]
        public void MultipleCasts() {
            Assert.AreEqual(
                @"() => 1 == (int)(object)1",
                ExpressionToCode.ToCode(() => 1 == (int)(object)1));
        }

        [Test]
        public void MultipleDots() {
            Assert.AreEqual(
                @"() => 3.ToString().ToString().Length > 0",
                ExpressionToCode.ToCode(() => 3.ToString().ToString().Length > 0));
        }

        [Test]
        public void NestedLambda() {
            Func<Func<int>, int> call = f => f();
            Assert.AreEqual(
                @"() => call(() => 42)",
                ExpressionToCode.ToCode(() => call(() => 42))
                ); //no params
            Assert.AreEqual(
                @"() => new[] { 37, 42 }.Select(x => x * 2)",
                ExpressionToCode.ToCode(() => new[] { 37, 42 }.Select(x => x * 2))
                ); //one param
            Assert.AreEqual(
                @"() => new[] { 37, 42 }.Select((x, i) => x * 2)",
                ExpressionToCode.ToCode(() => new[] { 37, 42 }.Select((x, i) => x * 2))
                ); //two params
        }

        bool Fizz(Func<int, bool> a) { return a(42); }
        bool Buzz(Func<int, bool> a) { return a(42); }
        bool Fizz(Func<string, bool> a) { return a("42"); }

        [Test]
        public void NestedLambda2() {
            Assert.AreEqual(
                @"() => Fizz(x => x == ""a"")",
                ExpressionToCode.ToCode(() => Fizz(x => x == "a"))
                );
            Assert.AreEqual(
                @"() => Fizz(x => x == 37)",
                ExpressionToCode.ToCode(() => Fizz(x => x == 37))
                );
        }

        [Test, Ignore("issue 14")]
        public void NestedLambda3() {
            Assert.AreEqual(
                @"() => Buzz(x => true)",
                ExpressionToCode.ToCode(() => Buzz(x => true))
                ); //easier case...
            Assert.AreEqual(
                @"() => Fizz((int x) => true)",
                ExpressionToCode.ToCode(() => Fizz((int x) => true))
                ); //hard case!
        }

        [Test]
        public void NewArrayAndExtensionMethod() {
            Assert.AreEqual(
                @"() => new[] { 1.0, 2.01, 3.5 }.SequenceEqual(new[] { 1.0, 2.01, 3.5 })",
                ExpressionToCode.ToCode(() => new[] { 1.0, 2.01, 3.5 }.SequenceEqual(new[] { 1.0, 2.01, 3.5 })));
        }

        [Test]
        public void NewMultiDimArray() {
            Assert.AreEqual(
                @"() => new int[3, 4].Length == 1",
                ExpressionToCode.ToCode(() => new int[3, 4].Length == 1));
        }

        [Test]
        public void NewObjectNotEqualsNewObject() {
            Assert.AreEqual(
                @"() => new object() != new object()",
                ExpressionToCode.ToCode(() => new object() != new object()));
        }

        [Test]
        public void NotOperator() {
            bool x = true;
            int y = 3;
            byte z = 42;
            Assert.AreEqual(
                @"() => ~(int)z == 0",
                ExpressionToCode.ToCode(() => ~(int)z == 0));
            Assert.AreEqual(
                @"() => ~y == 0",
                ExpressionToCode.ToCode(() => ~y == 0));
            Assert.AreEqual(
                @"() => !x",
                ExpressionToCode.ToCode(() => !x));
        }

        [Test]
        public void ObjectInitializers() {
            var s = new XmlReaderSettings {
                CloseInput = false,
                CheckCharacters = false
            };
            Assert.AreEqual(
                @"() => new XmlReaderSettings { CloseInput = s.CloseInput, CheckCharacters = s.CheckCharacters }.Equals(s)",
                ExpressionToCode.ToCode(
                    () => new XmlReaderSettings { CloseInput = s.CloseInput, CheckCharacters = s.CheckCharacters }.Equals(s)));
        }

        [Test]
        public void Quoted() {
            Assert.AreEqual(
                @"() => (Expression<Func<int, string, string>>)((n, s) => s + n.ToString()) != null",
                ExpressionToCode.ToCode(() => (Expression<Func<int, string, string>>)((n, s) => s + n.ToString()) != null));
        }

        [Test]
        public void Quoted2() {
            Assert.AreEqual(
                @"() => ExpressionToCode.ToCode(() => true).Length > 5",
                ExpressionToCode.ToCode(() => ExpressionToCode.ToCode(() => true).Length > 5));
        }

        [Test]
        public void QuotedWithAnonymous() {
            Assert.AreEqual(
                @"() => new[] { new { X = ""a"", Y = ""b"" } }.Select(o => o.X + o.Y).Single()",
                ExpressionToCode.ToCode(() => new[] { new { X = "a", Y = "b" } }.Select(o => o.X + o.Y).Single()));
        }

        [Test]
        public void StaticCall() {
            Assert.AreEqual(
                @"() => object.Equals((object)3, (object)0)",
                ExpressionToCode.ToCode(() => Equals(3, 0)));
        }

        [Test]
        public void ThisCall() {
            Assert.AreEqual(
                @"() => !Equals((object)3)",
                ExpressionToCode.ToCode(() => !Equals(3)));
        }

        [Test]
        public void ThisExplicit() {
            Assert.AreEqual(
                @"() => object.Equals(this, (object)3)",
                ExpressionToCode.ToCode(() => object.Equals(this, 3)));
        }

        [Test]
        public void TypedConstant() {
            Assert.AreEqual(
                @"() => new[] { typeof(int), typeof(string) }",
                ExpressionToCode.ToCode(() => new[] { typeof(int), typeof(string) }));
        }

        [Test]
        public void StaticCallImplicitCast() {
            Assert.AreEqual(
                @"() => object.Equals((object)3, (object)0)",
                ExpressionToCode.ToCode(() => Equals(3, 0)));
        }

        [Test]
        public void StaticMembers() {
            Assert.AreEqual(
                @"() => (DateTime.Now > DateTime.Now + TimeSpan.FromMilliseconds(10.001)).ToString() == ""False""",
                ExpressionToCode.ToCode(
                    () => (DateTime.Now > DateTime.Now + TimeSpan.FromMilliseconds(10.001)).ToString() == "False"));
        }

        [Test]
        public void Strings2() {
            var x = "X";
            const string y = "Y";
            Assert.AreEqual(
                @"() => x != ""Y"" && x.Length == ""Y"".Length && ""a"".Length == 1",
                ExpressionToCode.ToCode(() => x != y && x.Length == y.Length && "a".Length == 1));
        }

        [Test]
        public void StringAccessor() {
            Assert.AreEqual(
                @"() => ""abc""[1] == 'b'",
                ExpressionToCode.ToCode(() => "abc"[1] == 'b'));
        }

        [Test]
        public void StringConcat() {
            var x = "X";
            Assert.AreEqual(
                @"() => ((""a\n\\b"" ?? x) + x).Length == 2 ? false : true",
                ExpressionToCode.ToCode(() => (("a\n\\b" ?? x) + x).Length == 2 ? false : true));
        }

        [Test]
        public void ArgumentWithRefModifier() {
            var x = "a";
            Assert.AreEqual(
                @"() => MethodWithRefParam(ref x)",
                ExpressionToCode.ToCode(() => MethodWithRefParam(ref x)));
        }

        T MethodWithRefParam<T>(ref T input) { return input; }

        [Test]
        public void ArgumentWithOutModifier() {
            var x = "a";
            string y;
            Assert.AreEqual(
                @"() => MethodWithOutParam(ref x, out y)",
                ExpressionToCode.ToCode(() => MethodWithOutParam(ref x, out y)));
        }

        T MethodWithOutParam<T>(ref T input, out T output) { return output = input; }

        [Test]
        public void StaticMethodWithRefAndOutModifiers() {
            var x = "a";
            object y;
            Assert.AreEqual(
                @"() => ClassA.MethodWithOutAndRefParam(ref x, out y, 3)",
                ExpressionToCode.ToCode(() => ClassA.MethodWithOutAndRefParam(ref x, out y, 3)));
        }

        [Test]
        public void ConstructorMethodWithRefAndOutModifiers() {
            int x = 42;
            int y;
            Assert.AreEqual(
                @"() => new ClassA(ref x, out y))",
                ExpressionToCode.ToCode(() => new ClassA(ref x, out y)));
        }


        [Test]
        public void ExtensionMethodWithRefAndOutModifiers() {
            int x = 42;
            long y;
            Assert.AreEqual(
                @"() => new ClassA(ref x, out y))",
                ExpressionToCode.ToCode(() => DateTime.Now.AnExtensionMethod(ref x, 5, out y)));
        }


        [Test]
        public void DelegateCallWithRefAndOutModifiers() {
            int x = 42;
            int y;
            DelegateWithRefAndOut myDelegate = (ref int someVar, out int anotherVar) => anotherVar = someVar;
            Assert.AreEqual(
                @"() => new ClassA(ref x, out y))",
                ExpressionToCode.ToCode(() => myDelegate(ref x, out y)));
        }
        
        [Test]
        public void NewObject(){
        	    Assert.AreEqual(
                @"() => new object()",
                ExpressionToCode.ToCode(() => new Object()));
        }
    }

    public delegate int DelegateWithRefAndOut(ref int someVar, out int anotherVar);

    static class StaticHelperClass {
        public static long AnExtensionMethod(this DateTime date, ref int tickOffset, int dayOffset, out long alternateOut) {
            return alternateOut = date.AddDays(dayOffset).Ticks + tickOffset;
        }
    }

    class ClassA {
        public static int MethodWithOutAndRefParam<T>(ref T input, out object output, int x) {
            output = x == 4 ? default(object) : input;
            return x;
        }

        int x;
        public ClassA() { }
        public ClassA(ref int something, out int output) { output = x = something; }

        public void DoAssert() {
            x = 37;
            Assert.AreEqual(
                @"() => x != C()",
                ExpressionToCode.ToCode(() => x != C()));
            Assert.AreEqual(
                @"() => !object.ReferenceEquals(this, new ClassA())",
                ExpressionToCode.ToCode(() => !ReferenceEquals(this, new ClassA())));
            Assert.AreEqual(
                @"() => MyEquals(this) && !MyEquals(default(ClassA))",
                ExpressionToCode.ToCode(() => MyEquals(this) && !MyEquals(default(ClassA))));
        }

        int C() { return x + 5; }
        bool MyEquals(ClassA other) { return other != null && x == other.x; }
    }
}
