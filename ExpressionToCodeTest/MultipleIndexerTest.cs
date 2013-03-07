using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExpressionToCodeLib;
using NUnit.Framework;

namespace ExpressionToCodeTest
{
	public class MultipleIndexerTest
	{
		class Bar
		{
			public object this[string s] { get { return null; } }
			public object this[int i] { get { return null; } }
		}

		[Test]
		public void CanPrettyPrintVariousIndexers() { 
			Assert.AreEqual(
				"() => new Bar()[3] == new Bar()[\"three\"]",
				ExpressionToCode.ToCode(() => new Bar()[3] == new Bar()["three"])
				);
		}

	}
}
