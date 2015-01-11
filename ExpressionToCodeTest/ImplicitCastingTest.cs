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
        public void CharComp() {
            var c = 'c';
            Assert.AreEqual(
                @"() => c == 'b'",
                ExpressionToCode.ToCode(() => c == 'b'));
        }

        [Test, Ignore("issue 4")]
        public void DecimalImplicitCast() {
            var i = 1;
            Assert.AreEqual(
                @"() => (1m + -i > 0 || false)",
                ExpressionToCode.ToCode(() => (1m + -i > 0 || false)));
        }

        [Test, Ignore("issue 4")]
        public void StringImplicitConcat() {
            var i = 1;
            var x = "X";
            Assert.AreEqual(
                @"() => 1m + x + ""!!"" + i)",
                ExpressionToCode.ToCode(() => 1m + x + "!!" + i));
        }

        [Test, Ignore("issue 4")]
        public void NotImplicitCast() {
            byte z = 42;
            Assert.AreEqual(
                @"() => ~z == 0",
                ExpressionToCode.ToCode(() => ~z == 0));
        }
    }
}
