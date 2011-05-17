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
		static readonly object bla2 = "bla2";
		static readonly string bla2string = "bla2string";
		[Test]
		public void EqualsOpDetected() {
			Assert.AreEqual(EqualityExpressions.CheckForEquality(()=>bla == bla2), EqualityExpressionClass.EqualsOp);
		}
		[Test]
		public void NotEqualsOpDetected() {
			Assert.AreEqual(EqualityExpressions.CheckForEquality(() => bla != bla2), EqualityExpressionClass.NotEqualsOp);
		}
		[Test]
		public void EquatableDetected() {
			Assert.AreEqual(EqualityExpressions.CheckForEquality(() => ((IEquatable<string>)bla).Equals(bla2string) ), EqualityExpressionClass.EquatableEquals);
		}
	}
}
