using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExpressionToCodeLib;
using NUnit.Framework;

namespace ExpressionToCodeTest
{

	class HasIndexers
	{
		public object this[string s] { get { return null; } }
		public object this[int i] { get { return null; } }
	}

	public class MultipleIndexerTest
	{


		[Test]
		public void CanPrettyPrintVariousIndexers() { 
			Assert.AreEqual(
				"() => new HasIndexers()[3] == new HasIndexers()[\"three\"]",
				ExpressionToCode.ToCode(() => new HasIndexers()[3] == new HasIndexers()["three"])
				);
		}

	}
}
