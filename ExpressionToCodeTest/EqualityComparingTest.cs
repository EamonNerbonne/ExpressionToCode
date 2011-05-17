using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ExpressionToCodeLib;

#pragma warning disable 252,253
// ReSharper disable ConvertToConstant.Local
namespace ExpressionToCodeTest {
	[TestFixture]
	public class EqualityComparingTest {
		static readonly string bla = "bla";
		static readonly object bla2object = "bla2object";
		static readonly string bla2string = "bla2string";
		[Test]
		public void EqualsOpDetected() {
			Assert.AreEqual(EqualityExpressionClass.EqualsOp, EqualityExpressions.CheckForEquality(() => bla == bla2object));
		}
		[Test]
		public void NotEqualsOpDetected() {
			Assert.AreEqual(EqualityExpressionClass.NotEqualsOp, EqualityExpressions.CheckForEquality(() => bla != bla2object));
		}
		[Test]
		public void EquatableDetected() { //TODO: makes interesting expressiontocode test too
			Assert.AreEqual(EqualityExpressionClass.EquatableEquals, EqualityExpressions.CheckForEquality(() => ((IEquatable<string>)bla).Equals(bla2string)));
		}
		[Test]
		public void NoneDetected() {
			Assert.AreEqual(EqualityExpressionClass.None, EqualityExpressions.CheckForEquality(() => bla.StartsWith("bla")));
		}

		[Test]
		public void ObjectEqualsDetected() {
			Assert.AreEqual(EqualityExpressionClass.ObjectEquals, EqualityExpressions.CheckForEquality(() => bla2object.Equals(bla2object)));
			var anon = new { X = bla, Y = bla2object };
			Assert.AreEqual(EqualityExpressionClass.ObjectEquals, EqualityExpressions.CheckForEquality(() => anon.Equals(anon)));
		}
		[Test]
		public void ObjectEqualsStaticDetected() {
			Assert.AreEqual(EqualityExpressionClass.ObjectEqualsStatic, EqualityExpressions.CheckForEquality(() => object.Equals(bla,null)));
		}
		[Test]
		public void ObjectReferenceEqualsDetected() {
			Assert.AreEqual(EqualityExpressionClass.ObjectReferenceEquals, EqualityExpressions.CheckForEquality(() => object.ReferenceEquals(bla2object, bla2object)));
		}
		[Test]
		public void SequenceEqualsDetected() {
			Assert.AreEqual(EqualityExpressionClass.SequenceEqual, EqualityExpressions.CheckForEquality(() =>bla.AsEnumerable().SequenceEqual(bla2string) ));
		}
	}
}
