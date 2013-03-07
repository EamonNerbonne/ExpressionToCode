using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using ExpressionToCodeLib;
using NUnit.Framework;

namespace ExpressionToCodeTest
{
	public class Parent
	{
		public class Child
		{
		}
	}

	class NestedClassTest
	{
		[Test]
		public void NestedTypeNamesAreCorrectlyRegenerated()
		{
			string code = ExpressionToCode.ToCode(() => null as Parent.Child);
			Assert.That(code, Is.EqualTo("() => null as Parent.Child"));
		}
	}
}
