using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ExpressionToCodeLib;

namespace ExpressionToCodeTest {
	class GenericClass<T> {
		T val;
		public GenericClass(T pVal) {
			val = pVal;
		}
		public GenericClass() { val = default(T); }

		public T Value { get { return val; } }
		public void Reset(T pVal) { val = pVal; }
		public void Reset() { val = default(T); }
		public bool IsSet() { return Equals(default(T), val); }
		public static T GetDefault() { return default(T); }

		public bool IsSubClass<U>() where U : T { return val is U; }
		public bool IsSubEquatable<U>(U other) where U : T, IEquatable<T> { return other.Equals(val); }
	}

	class GenericSubClass<T, U> : GenericClass<T> where T : IEnumerable<U> {
		public GenericSubClass(T val) : base(val) { }
		public bool IsEmpty { get { return !Value.Any(); } }
	}

	class StaticTestClass {
		public static T Identity<T>(T val) { return val; }
		public static bool IsType<T, U>(T val) { return val is U; }
		public static bool TEqualsInt<T>(int val) { return val is T; }
		public static bool TwoArgsOneGeneric<T>(int val, T other) { return val.Equals(other); }
		public static bool TwoArgsTwoGeneric<T>(T val, T other) { return val.Equals(other); }
	}

	[TestFixture]
	public class TestGenerics {
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
	}
}
