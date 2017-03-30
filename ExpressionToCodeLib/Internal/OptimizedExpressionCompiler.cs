using System;
using System.Linq.Expressions;

namespace ExpressionToCodeLib.Internal
{
    class OptimizedExpressionCompiler : IExpressionCompiler
    {
        public Func<T> Compile<T>(Expression<Func<T>> expression) => FastExpressionCompiler.ExpressionCompiler.Compile(expression);
    }
}
