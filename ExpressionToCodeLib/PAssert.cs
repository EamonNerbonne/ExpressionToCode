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

        /// <summary>
        /// Evaluates an assertion and throws an exception the assertion it returns false or throws an exception.
        /// The exception includes the code of the assertion annotated with runtime values for its sub-expressions.
        /// </summary>
        public static void That(Expression<Func<bool>> assertion, string msg = null)
        {
            ExpressionToCodeConfiguration.CurrentConfiguration.Assert(assertion, msg);
        }
    }
}
