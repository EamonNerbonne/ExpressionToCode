using System;
using System.Linq.Expressions;

namespace ExpressionToCodeLib
{
    public static class PAssert
    {
        [Obsolete("Prefer PAssert.That: IsTrue is provided for compatibility with PowerAssert.NET")]
        public static void IsTrue(Expression<Func<bool>> assertion)
        {
            That(assertion);
        }

        public static void That(Expression<Func<bool>> assertion, string msg = null, bool emit = false)
        {
            var compiled = emit ? ExpressionCompiler.Compile(assertion) : assertion.Compile();
            bool ok = false;
            try {
                ok = compiled();
            } catch (Exception e) {
                throw Err(assertion, msg ?? "failed with exception", e);
            }
            if (!ok) {
                throw Err(assertion, msg ?? "failed", null);
            }
        }

        static Exception Err(Expression<Func<bool>> assertion, string msg, Exception innerException)
            => UnitTestingFailure.AssertionExceptionFactory(ValuesOnStalksCodeAnnotator.AnnotatedToCode(assertion.Body, msg, true), innerException);
    }
}
