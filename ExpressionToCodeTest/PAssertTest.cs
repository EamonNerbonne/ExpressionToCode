using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ExpressionToCodeLib;

namespace ExpressionToCodeTest {
    [TestFixture]
    public class PAssertTest {
        [Test]
        public void TestBasicStalks() {
            var msgLines = PAssertLines(
                () =>
                    PAssert.That(
                        () =>
                            TimeSpan.FromMilliseconds(10.0).CompareTo(TimeSpan.FromMinutes(1.0)) > 0
                        ));
            Assert.That(msgLines[0], Is.EqualTo(@"TimeSpan.FromMilliseconds(10.0).CompareTo(TimeSpan.FromMinutes(1.0)) > 0  :  failed"));
            Assert.That(
                string.Join("\n", msgLines),
                Is.EqualTo(
                    @"TimeSpan.FromMilliseconds(10.0).CompareTo(TimeSpan.FromMinutes(1.0)) > 0  :  failed
                 │                  │                   │
                 │                  │                   00:01:00
                 │                  -1
                 00:00:00.0100000
".Replace("\r", "").Trim()));
        }

        [Test]
        public void NoValuesForBoringCasts() {
            var msgLines = PAssertLines(
                () =>
                    PAssert.That(
                        () =>
                            Equals(3, 4)
                        ));
            Assert.That(msgLines[0], Contains.Substring("failed"));
            Assert.That(msgLines[0], Contains.Substring("Equals"));
            Assert.That(msgLines.Length, Is.EqualTo(1));
        }

        [Test]
        public void ValuesForNonBoringCasts() {
            ulong x = ulong.MaxValue;
            var msgLines = PAssertLines(
                () => PAssert.That(
                    () => 0 == (ulong)(uint)x
                    ));
            Assert.That(msgLines[0], Is.EqualTo(@"0 == (ulong)(uint)x  :  failed"));
            Assert.That(msgLines[1].Count(c => c == '│'), Is.EqualTo(3)); //for x, x+cast,x+cast+cast, NOT for ==, NOT for constant 0
        }

        [Test]
        public void AppendsFailedOnFailure() {
            var msgLines = PAssertLines(() => PAssert.That(() => false));
            Assert.That(msgLines[0], Is.EqualTo(@"false  :  failed"));
            Assert.That(msgLines.Length, Is.EqualTo(1));
        }

        [Test]
        public void AppendsSingleLineMessageOnFailure() {
            var msgLines = PAssertLines(() => PAssert.That(() => false, "oops"));
            Assert.That(msgLines[0], Is.EqualTo(@"false  :  oops"));
            Assert.That(msgLines.Length, Is.EqualTo(1));
        }

        [Test]
        public void AppendsSingleLineMessageWithNewlineOnFailure() {
            var msgLines = PAssertLines(() => PAssert.That(() => false, "oops\n"));
            Assert.That(msgLines[0], Is.EqualTo(@"false  :  oops"));
            Assert.That(msgLines.Length, Is.EqualTo(1));
        }

        [Test]
        public void AppendsSingleLineMessageBeforeStalks() {
            var x = 0;
            var msgLines = PAssertLines(() => PAssert.That(() => x == 1, "oops\n"));
            Assert.That(msgLines[0], Is.EqualTo(@"x == 1  :  oops"));
            Assert.That(msgLines.Length, Is.EqualTo(3)); //expression, empty, NOT x==1, x
        }

        [Test]
        public void PrependsMultiLineMessage() {
            var x = 0;
            var msgLines = PAssertLines(() => PAssert.That(() => x == 1, "oops\nagain"));
            Assert.That(msgLines[0], Is.EqualTo(@"oops"));
            Assert.That(msgLines[1], Is.EqualTo(@"again"));
            Assert.That(msgLines[2], Is.EqualTo(@"x == 1"));
            Assert.That(msgLines.Length, Is.EqualTo(5)); //oops,again,expression, empty, NOT x==1, x
        }

        static string[] PAssertLines(TestDelegate action) {
            var exc = Assert.Catch(action);
            return exc.Message.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
