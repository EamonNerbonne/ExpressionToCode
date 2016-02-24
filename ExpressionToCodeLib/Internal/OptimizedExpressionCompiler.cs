using System;
using System.Linq.Expressions;

namespace ExpressionToCodeLib.Internal
{
    internal class OptimizedExpressionCompiler : IExpressionCompiler
    {
        public Func<T> Compile<T>(Expression<Func<T>> expression) { return OptimizedExpressionCompilerImpl.TryCompile(expression) ?? expression.Compile(); }
    }
}