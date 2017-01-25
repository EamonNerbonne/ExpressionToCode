using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace ExpressionToCodeLib
{

    /// <summary>
    /// Intended to be used as a static import; i.e. via "using static ExpressionToCodeLib.ExpressionAssertions;"
    /// </summary>
    public static class ExpressionAssertions
    {
        /// <summary>
        /// Evaluates an assertion and throws an exception the assertion it returns false or throws an exception.
        /// The exception includes the code of the assertion annotated with runtime values for its sub-expressions.
        /// 
        /// This is identical to PAssert.That(()=>...).
        /// 
        /// If you want to change the layout of the value annotations, see ExpressionToCodeConfiguration.GlobalAssertionConfiguration
        /// </summary>
        public static void Expect(Expression<Func<bool>> assertion, string msg = null)
        {
            ExpressionToCodeConfiguration.GlobalAssertionConfiguration.Assert(assertion, msg);
        }
    }
}
