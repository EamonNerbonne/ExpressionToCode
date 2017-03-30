using System;
using System.Linq.Expressions;

namespace ExpressionToCodeLib.Internal
{
    class DefaultExpressionCompiler : IExpressionCompiler
    {
        public Func<T> Compile<T>(Expression<Func<T>> expression) => expression.Compile();
    }
}
