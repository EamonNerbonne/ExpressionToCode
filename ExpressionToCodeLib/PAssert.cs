using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ExpressionToCodeLib {
	public static class PAssert {
		public static void IsTrue(Expression<Func<bool>> assertion) { That(assertion, "PAssert.IsTrue failed for:"); }

		public static void That(Expression<Func<bool>> assertion, string msg = null) {
			var compiled = assertion.Compile();
			bool? ok;
			try {
				ok = compiled();
			} catch (Exception e) {
				throw UnitTestingFailure.AssertionExceptionFactory((msg == null ? "" : msg + "\n") +
					ExpressionToCode.AnnotatedToCode(assertion.Body), e);
			}
			if (ok == false)
				throw UnitTestingFailure.AssertionExceptionFactory(msg + "\n" +
					ExpressionToCode.AnnotatedToCode(assertion.Body), null);
		}
	}
}