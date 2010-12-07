using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace ExpressionToCodeLib {
	public static class PAssert {
		public static void IsTrue(Expression<Func<bool>> assertion) {
			That(assertion,"PAssert.IsTrue failed for:");
		}
		public static void That(Expression<Func<bool>> assertion, string msg = null) {
			if (!assertion.Compile()())
				throw new PAssertFailedException((msg ?? "PAssert.That failed for:") + "\n\n" + ExpressionToCode.AnnotatedToCode(assertion.Body));
		}
	}
}
