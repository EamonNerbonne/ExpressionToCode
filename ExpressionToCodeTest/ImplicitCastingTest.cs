using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExpressionToCodeLib;
using NUnit.Framework;

namespace ExpressionToCodeTest {
	[TestFixture]
	public class ImplicitCastingTest {
		[Test]
		public void CharNoCast() {
			Assert.AreEqual(
				@"() => ""abc""[1] == 'b'",
				ExpressionToCode.ToCode(() => "abc"[1] == 'b'));
		}

		[Test]
		public void StringsImplicitCast() {
			var i = 1;
			var x = "X";
			Assert.AreEqual(
				@"() => ((""a\n\\b"" ?? x) + x).Length == 2 ? false : true && (1m + -i > 0 || false)",
				ExpressionToCode.ToCode(() => (("a\n\\b" ?? x) + x).Length == 2 ? false : true && (1m + -i > 0 || false)));
		}

		[Test]
		public void NotImplicitCast() {
			byte z = 42;
			Assert.AreEqual(
				@"() => ~z == 0",
				ExpressionToCode.ToCode(() => ~z == 0));
		}
	}
}
