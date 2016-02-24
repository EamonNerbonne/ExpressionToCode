using System;
using System.Linq;
using ExpressionToCodeLib;
using Xunit;

namespace ExpressionToCodeTest
{
    static class Assert2 { }

    public class PAssertTest
    {
        [Fact]
        public void TestBasicStalks()
        {
            var msgLines = PAssertLines(
                () =>
                    PAssert.That(
                        () =>
                            TimeSpan.FromMilliseconds(10.0).CompareTo(TimeSpan.FromMinutes(1.0)) > 0
                        ));
            Assert.Equal(@"TimeSpan.FromMilliseconds(10.0).CompareTo(TimeSpan.FromMinutes(1.0)) > 0  :  failed", msgLines[0]);
            var expectedMessage = @"
TimeSpan.FromMilliseconds(10.0).CompareTo(TimeSpan.FromMinutes(1.0)) > 0  :  failed
                 │                  │                   │
                 │                  │                   00:01:00
                 │                  -1
                 00:00:00.0100000
";
            Assert.Equal(expectedMessage.Replace("\r", "").Trim(), string.Join("\n", msgLines));
        }

        [Fact]
        public void NoValuesForBoringCasts()
        {
            var msgLines = PAssertLines(
                () =>
                    PAssert.That(
                        () =>
                            Equals(3, 4)
                        ));
            Assert.Contains("failed", msgLines[0]);
            Assert.Contains("Equals", msgLines[0]);
            Assert.Equal(1, msgLines.Length);
        }

        [Fact]
        public void ValuesForNonBoringCasts()
        {
            ulong x = ulong.MaxValue;
            var msgLines = PAssertLines(
                () => PAssert.That(
                    () => 0 == (ulong)(uint)x
                    ));
            Assert.Equal((@"0 == (ulong)(uint)x  :  failed"), (object)msgLines[0]);
            Assert.Equal((3), (object)msgLines[1].Count(c => c == '│'));
        }

        [Fact]
        public void AppendsFailedOnFailure()
        {
            var msgLines = PAssertLines(() => PAssert.That(() => false));
            Assert.Equal((@"false  :  failed"), (object)msgLines[0]);
            Assert.Equal((1), (object)msgLines.Length);
        }

        [Fact]
        public void AppendsSingleLineMessageOnFailure()
        {
            var msgLines = PAssertLines(() => PAssert.That(() => false, "oops"));
            Assert.Equal((@"false  :  oops"), (object)msgLines[0]);
            Assert.Equal((1), (object)msgLines.Length);
        }

        [Fact]
        public void AppendsSingleLineMessageWithNewlineOnFailure()
        {
            var msgLines = PAssertLines(() => PAssert.That(() => false, "oops\n"));
            Assert.Equal((@"false  :  oops"), (object)msgLines[0]);
            Assert.Equal((1), (object)msgLines.Length);
        }

        [Fact]
        public void AppendsSingleLineMessageBeforeStalks()
        {
            var x = 0;
            var msgLines = PAssertLines(() => PAssert.That(() => x == 1, "oops\n"));
            Assert.Equal((@"x == 1  :  oops"), (object)msgLines[0]);
            Assert.Equal((3), (object)msgLines.Length);
        }

        [Fact]
        public void PrependsMultiLineMessage()
        {
            var x = 0;
            var msgLines = PAssertLines(() => PAssert.That(() => x == 1, "oops\nagain"));
            Assert.Equal((@"oops"), (object)msgLines[0]);
            Assert.Equal((@"again"), (object)msgLines[1]);
            Assert.Equal((@"x == 1"), (object)msgLines[2]);
            Assert.Equal((5), (object)msgLines.Length);
        }

        static string[] PAssertLines(Action action)
        {
            var exc = Assert.ThrowsAny<Exception>(action);
            return exc.Message.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
