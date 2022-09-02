namespace ExpressionToCodeTest;

public class ArrayAccessTests
{
    [Fact]
    public void TestSingleDimensionalArrayIndexExpressionWithLambda()
    {
        var param = Expression.Parameter(typeof(string[]), "a");
        var expr = Expression.Lambda(
            Expression.ArrayIndex(param, Expression.Constant(1)),
            param
        );
        Assert.Equal(
            "a => a[1]",
            ExpressionToCode.ToCode(expr)
        );
    }

    [Fact]
    public void TestSingleDimensionalArrayAccessExpressionWithLambda()
    {
        var param = Expression.Parameter(typeof(string[]), "a");
        var expr = Expression.Lambda(
            Expression.ArrayAccess(param, Expression.Constant(1)),
            param
        );
        Assert.Equal(
            "a => a[1]",
            ExpressionToCode.ToCode(expr)
        );
    }
}
