namespace ExpressionToCodeLib.Internal;

sealed class DotnetExpressionCompiler : IExpressionCompiler
{
    public Func<T> Compile<T>(Expression<Func<T>> expression)
        => expression.Compile(true);

    public Delegate Compile(LambdaExpression expression)
        => expression.Compile(true);
}
