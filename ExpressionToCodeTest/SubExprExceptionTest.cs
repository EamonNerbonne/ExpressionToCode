using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExpressionToCodeLib;
using NUnit.Framework;

namespace ExpressionToCodeTest
{
	public class FailingClass
	{
		public static int SomeFunction() { throw new Exception(); }
	}

	class SubExprExceptionTest
	{
		[Test]
		public void ExceptionDoesntCauseFailure()
		{
			
			Assert.AreEqual(
@"() => FailingClass.SomeFunction()
                         │
                         throws System.Exception
", ExpressionToCode.AnnotatedToCode(() => FailingClass.SomeFunction()));
		}

	}
}
