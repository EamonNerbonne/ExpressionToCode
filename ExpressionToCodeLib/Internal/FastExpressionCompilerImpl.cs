using System;
using System.Linq.Expressions;
using FastExpressionCompiler;

namespace ExpressionToCodeLib.Internal
{
    sealed class FastExpressionCompilerImpl : IExpressionCompiler
    {
        static readonly DotnetExpressionCompiler fallback = new DotnetExpressionCompiler();

        public Func<T> Compile<T>(Expression<Func<T>> expression)
            => ExpressionCompiler.TryCompile<Func<T>>(expression) ?? fallback.Compile(expression);

        public Delegate Compile(LambdaExpression expression)
            => ExpressionCompiler.TryCompile<Delegate>(expression) ?? fallback.Compile(expression);
    }
}
