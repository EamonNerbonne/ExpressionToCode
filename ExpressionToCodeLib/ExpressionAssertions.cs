using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace ExpressionToCodeLib
{
    public static class ExpressionAssertions
    {
        public static void Assert(Expression<Func<bool>> assertion, string msg = null)
        {
            ExpressionToCodeConfiguration.CurrentConfiguration.Assert(assertion, msg);
        }
    }
}
