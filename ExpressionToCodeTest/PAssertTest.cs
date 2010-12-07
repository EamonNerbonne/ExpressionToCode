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
			var exc = Assert.Throws<PAssertFailedException>(() => {
				PAssert.IsTrue(() => TimeSpan.FromMilliseconds(10.0).CompareTo(TimeSpan.FromMinutes(1.0)) > 0);
			});
			var msgLines = exc.Message.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
			Assert.That(msgLines[0], Contains.Substring("failed"));
			Assert.That(msgLines[1], Is.EqualTo(@"TimeSpan.FromMilliseconds(10.0).CompareTo(TimeSpan.FromMinutes(1.0)) > 0"));
			Assert.That(string.Join("\n", msgLines.Skip(1).ToArray()), Is.EqualTo(@"
TimeSpan.FromMilliseconds(10.0).CompareTo(TimeSpan.FromMinutes(1.0)) > 0
                |                   |                   |            |
                |                   |                   |            false
                |                   |                   00:01:00
                |                   -1
                00:00:00.0100000
".Replace("\r","").Trim()));
		}
	}
}
