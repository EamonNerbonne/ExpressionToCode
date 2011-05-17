using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ExpressionToCodeLib;

#pragma warning disable 252,253
// ReSharper disable NegativeEqualityExpression
// ReSharper disable EqualExpressionComparison
// ReSharper disable ConvertToConstant.Local
// ReSharper disable RedundantCast
namespace ExpressionToCodeTest {
	[TestFixture]
	public class EqualityComparingTest {
		static readonly string bla = "bla";
		static readonly object bla2object = "bla2object";
		static readonly string bla2string = "bla2string";
		[Test]
		public void EqualsOpDetected() {
			Assert.AreEqual(EqualityExpressionClass.EqualsOp, EqualityExpressions.CheckForEquality(() => bla == bla2object));
			Assert.AreEqual(EqualityExpressionClass.EqualsOp, EqualityExpressions.CheckForEquality(() => new DateTime(2011, 05, 17) == DateTime.Today));
		}
		[Test]
		public void NotEqualsOpDetected() {
			Assert.AreEqual(EqualityExpressionClass.NotEqualsOp, EqualityExpressions.CheckForEquality(() => bla != bla2object));
			Assert.AreEqual(EqualityExpressionClass.NotEqualsOp, EqualityExpressions.CheckForEquality(() => new DateTime(2011, 05, 17) != DateTime.Today));
		}
		[Test]
		public void EquatableDetected() { //TODO: makes interesting expressiontocode test too
			Assert.AreEqual(EqualityExpressionClass.EquatableEquals, EqualityExpressions.CheckForEquality(() => ((IEquatable<string>)bla).Equals(bla2string)));
			Assert.AreEqual(EqualityExpressionClass.EquatableEquals, EqualityExpressions.CheckForEquality(() => bla.Equals(bla2string)));
			Assert.AreEqual(EqualityExpressionClass.EquatableEquals, EqualityExpressions.CheckForEquality(() => new DateTime(2011, 05, 17).Equals(DateTime.Today)));
		}
		[Test]
		public void NoneDetected() {
			Assert.AreEqual(EqualityExpressionClass.None, EqualityExpressions.CheckForEquality(() => bla.StartsWith("bla")));
			Assert.AreEqual(EqualityExpressionClass.None, EqualityExpressions.CheckForEquality(() => !(bla == bla2object)));
			Assert.AreEqual(EqualityExpressionClass.None, EqualityExpressions.CheckForEquality(() => DateTime.Equals(new DateTime(2011, 05, 17), DateTime.Today)));// no match since specific method
			Assert.AreEqual(EqualityExpressionClass.None, EqualityExpressions.CheckForEquality(() => !ReferenceEquals(null, null)));
		}
		[Test]
		public void ObjectEqualsDetected() {
			Assert.AreEqual(EqualityExpressionClass.ObjectEquals, EqualityExpressions.CheckForEquality(() => bla2object.Equals(bla2object)));
			var anon = new { X = bla, Y = bla2object };
			Assert.AreEqual(EqualityExpressionClass.ObjectEquals, EqualityExpressions.CheckForEquality(() => anon.Equals(anon)));
		}
		[Test]
		public void ObjectEqualsStaticDetected() {
			Assert.AreEqual(EqualityExpressionClass.ObjectEqualsStatic, EqualityExpressions.CheckForEquality(() => Equals(bla, null)));
			Assert.AreEqual(EqualityExpressionClass.ObjectEqualsStatic, EqualityExpressions.CheckForEquality(() => Equals(null, 42)));
		}
		[Test]
		public void ObjectReferenceEqualsDetected() {
			Assert.AreEqual(EqualityExpressionClass.ObjectReferenceEquals, EqualityExpressions.CheckForEquality(() => ReferenceEquals(bla2object, bla2object)));
			Assert.AreEqual(EqualityExpressionClass.ObjectReferenceEquals, EqualityExpressions.CheckForEquality(() => ReferenceEquals(null, null)));
		}
		[Test]
		public void SequenceEqualsDetected() {
			Assert.AreEqual(EqualityExpressionClass.SequenceEqual, EqualityExpressions.CheckForEquality(() => bla.AsEnumerable().SequenceEqual(bla2string)));
			Assert.AreEqual(EqualityExpressionClass.SequenceEqual, EqualityExpressions.CheckForEquality(() => new[] { 'b', 'l', 'a' }.SequenceEqual(bla2string)));
		}

		static Tuple<EqualityExpressionClass, bool>[] eqclasses(params EqualityExpressionClass[] classes) {
			return classes.Select(eqClass => Tuple.Create(eqClass, false)).ToArray();
		}

		[Test]
		public void StringEqDisagreement() {
			Assert.That(
				EqualityExpressions.DisagreeingEqualities(() => object.ReferenceEquals(1000.ToString(), 10 + "00")).ToArray(),
				Is.EquivalentTo(eqclasses(
					EqualityExpressionClass.EqualsOp, EqualityExpressionClass.NotEqualsOp, EqualityExpressionClass.ObjectEquals, EqualityExpressionClass.ObjectEqualsStatic,
					EqualityExpressionClass.EquatableEquals,
					EqualityExpressionClass.SequenceEqual
#if DOTNET40
, EqualityExpressionClass.StructuralEquals
#endif
)));
			Assert.That(
				EqualityExpressions.DisagreeingEqualities(() => 1000.ToString().Equals(10 + "00")).ToArray(),
				Is.EquivalentTo(eqclasses(EqualityExpressionClass.ObjectReferenceEquals)));
		}

		[Test]
		public void DtRefEqDisagreement() {
			Assert.That(
				EqualityExpressions.DisagreeingEqualities(() => object.ReferenceEquals(new DateTime(2011, 05, 17), new DateTime(2011, 05, 17))).ToArray(),
				Is.EquivalentTo(eqclasses(
					EqualityExpressionClass.EqualsOp, EqualityExpressionClass.NotEqualsOp, EqualityExpressionClass.ObjectEquals, EqualityExpressionClass.ObjectEqualsStatic,
					EqualityExpressionClass.EquatableEquals
#if DOTNET40
, EqualityExpressionClass.StructuralEquals
#endif
)));
		}
		[Test]
		public void DtEqDisagreement() {
			Assert.That(
				EqualityExpressions.DisagreeingEqualities(() => new DateTime(2011, 05, 17).Equals(new DateTime(2011, 05, 17))).ToArray(),
				Is.EquivalentTo(eqclasses(EqualityExpressionClass.ObjectReferenceEquals)));
		}
	}
}
