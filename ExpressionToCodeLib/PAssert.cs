#if true
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ExpressionToCodeLib
{
    public static class PAssert
    {
        [Obsolete("Prefer PAssert.That: IsTrue is provided for compatiblity with PowerAssert.NET")]
        public static void IsTrue(Expression<Func<bool>> assertion)
        {
            That(assertion);
        }

        public static void That(Expression<Func<bool>> assertion, string msg = null)
        {
            var compiled = assertion.Compile();
            bool ok = false;
            try {
                ok = compiled();
            } catch(Exception e) {
                throw Err(assertion, msg ?? "failed with exception", e);
            }
            if(!ok) {
                throw Err(assertion, msg ?? "failed", null);
            }
        }

        static Exception Err(Expression<Func<bool>> assertion, string msg, Exception innerException) => UnitTestingFailure.AssertionExceptionFactory(ExpressionToCode.AnnotatedToCode(assertion.Body, msg, true), innerException);
    }
}

#else 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ExpressionToCodeLib {
	public static class PAssert {
		public static void IsTrue(Expression<Func<bool>> assertion) { That(assertion, "PAssert.IsTrue failed for:"); }

        private const string FullNewLine = "\r\n";

        private static string MergeExceptionMessages(string multilineExpressionDisplay, string customMsg, Exception e = null)
        {
            if (string.IsNullOrEmpty(customMsg)) {
                if (e != null) {
                    return "Expression threw: " + e.ToString() + FullNewLine + multilineExpressionDisplay;
                }
                customMsg = "failed";
            }
            else if (customMsg.Contains('\n')) {
                return customMsg + (customMsg.EndsWith("\n") ? FullNewLine : string.Empty) + multilineExpressionDisplay;
            }

            int insertPos = multilineExpressionDisplay.IndexOfAny(new char[] { '\r', '\n' });
            if (insertPos >= 0) {
                return multilineExpressionDisplay.Insert(insertPos, " " + customMsg);
            }
            return multilineExpressionDisplay + " " + customMsg;
        }

		public static void That(Expression<Func<bool>> assertion, string msg = null) {
			var compiled = assertion.Compile();
			bool? ok;
			try {
				ok = compiled();
			} catch (Exception e) {
                msg = MergeExceptionMessages(ExpressionToCode.AnnotatedToCode(assertion.Body), msg, e);
                throw UnitTestingFailure.AssertionExceptionFactory(msg, e);
			}
            if (ok == false) {
                msg = MergeExceptionMessages(ExpressionToCode.AnnotatedToCode(assertion.Body), msg);
                throw UnitTestingFailure.AssertionExceptionFactory(msg, null);
            }
		}
	}
}
#endif
