using System;
using System.Linq.Expressions;

namespace ExpressionToCodeLib.Internal
{
    class DotnetExpressionCompiler : IExpressionCompiler
    {
        public Func<T> Compile<T>(Expression<Func<T>> expression)
#if dotnet_core
            => expression.Compile(true);
#else
            => expression.Compile();
#endif
    }
}
