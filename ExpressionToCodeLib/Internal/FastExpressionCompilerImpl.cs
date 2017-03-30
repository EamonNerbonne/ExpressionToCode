using System;
using System.Linq.Expressions;

namespace ExpressionToCodeLib.Internal
{
    class FastExpressionCompilerImpl : IExpressionCompiler
    {
        public Func<T> Compile<T>(Expression<Func<T>> expression)
            => FastExpressionCompiler.ExpressionCompiler.Compile(expression);
    }
}
