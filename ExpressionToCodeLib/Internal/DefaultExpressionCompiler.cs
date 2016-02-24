using System;
using System.Linq.Expressions;

namespace ExpressionToCodeLib.Internal
{
    internal class DefaultExpressionCompiler : IExpressionCompiler
    {
        public Func<T> Compile<T>(Expression<Func<T>> expression) { return expression.Compile(); }
    }
}