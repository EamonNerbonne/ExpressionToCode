using System;
using System.Collections.Generic;
using System.Linq;
using ExpressionToCodeLib.Internal;

namespace ExpressionToCodeLib
{
    public static class ExpressionTreeCompilers
    {
        /// <summary>
        /// This compiler uses the .net built-in Expression.Compile() method to compile an expression tree.
        /// </summary>
        public static readonly IExpressionCompiler DotnetExpressionCompiler = new DotnetExpressionCompiler();

        /// <summary>
        /// This expression tree compiler should have the same semantics as the .net built-in Expression.Compile() method, but it's faster.
        /// It only supports a subset of parameterless lambdas.  
        /// Unsupported expressions fall-back to the builtin Expression.Compile methods.
        /// This compiler is relatively new, so if anything breaks, consider using the DotnetExpressionCompiler.
        /// </summary>
        public static readonly IExpressionCompiler FastExpressionCompiler =
            new FastExpressionCompilerImpl();
    }
}
