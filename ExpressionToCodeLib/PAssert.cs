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

        public static void PAssert(Expression<Func<bool>> assertion, string msg = null)
        {
        	That(assertion, msg);
        }
        
        public static void That(Expression<Func<bool>> assertion, string msg = null)
        {
            var config = ExpressionToCodeConfiguration.CurrentConfiguration;

            config.Assert(assertion, msg);
        }

    }
}
