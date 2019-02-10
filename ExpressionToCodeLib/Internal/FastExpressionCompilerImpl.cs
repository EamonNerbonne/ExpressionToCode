﻿using System;
using System.Linq.Expressions;
using FastExpressionCompiler;

namespace ExpressionToCodeLib.Internal
{
    sealed class FastExpressionCompilerImpl : IExpressionCompiler
    {
        static readonly DotnetExpressionCompiler fallback = new DotnetExpressionCompiler();

        public Func<T> Compile<T>(Expression<Func<T>> expression)
            => expression.TryCompile<Func<T>>() ?? fallback.Compile(expression);

        public Delegate Compile(LambdaExpression expression)
            => expression.TryCompile<Delegate>() ?? fallback.Compile(expression);
    }
}
