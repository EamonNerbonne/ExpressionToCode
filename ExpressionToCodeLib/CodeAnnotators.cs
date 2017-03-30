using System;
using System.Collections.Generic;
using System.Linq;
using ExpressionToCodeLib.Internal;

namespace ExpressionToCodeLib
{
    public static class CodeAnnotators
    {
        /// <summary>
        /// This code annotator places sub-expression values below the stringified expression and connects the value to the appropriate
        /// place in the expression with an ascii-art line. It works reasonably well for short expression, but large expressions are unwieldy and hard to read.
        /// This works best in monospace fonts.
        /// </summary>
        public static readonly ICodeAnnotator ValuesOnStalksCodeAnnotator = new ValuesOnStalksCodeAnnotator();

        /// <summary>
        /// This code annotator summarizes sub-expression values on separate lines below the stringified expression.  
        /// This works reasonably well even for large expressions because the sub-expressions are represented compactly and there's no need for overal alignment.
        /// </summary>
        public static readonly ICodeAnnotator SubExpressionPerLineCodeAnnotator = new SubExpressionPerLineCodeAnnotator();
    }
}
