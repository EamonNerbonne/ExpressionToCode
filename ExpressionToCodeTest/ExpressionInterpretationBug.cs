//requires .net 4.7.2, but not testing older than 4.8 anymore; so this is effectively always on

using FastExpressionCompiler;

namespace ExpressionToCodeTest;

/// <summary>
///     Repro for https://github.com/dotnet/runtime/issues/29673
/// </summary>
public class ExpressionInterpretationBug
{
    struct AStruct
    {
        public int AValue;
    }

    class SomethingMutable
    {
        public AStruct AStructField;
    }

    static Expression<Func<T>> Expr<T>(Expression<Func<T>> e)
        => e;

    [Fact]
    public void MemberMemberBindingForStructsCompilesOk()
    {
        var expr_compiled = Expr(() => new SomethingMutable { AStructField = { AValue = 2 } }).Compile(false)();
        Assert.Equal(2, expr_compiled.AStructField.AValue);
    }
#if!NETFRAMEWORK
    [Fact]
    public void MemberMemberBindingForStructsInterpretsWrong_BUGBUG()
    {
        var expr_interpreted = Expr(() => new SomethingMutable { AStructField = { AValue = 2 } }).Compile(true)();
        //Assert.Equal(2, expr_interpreted.AStructField.AValue);  //this should hold, but instead:
        Assert.Equal(0, expr_interpreted.AStructField.AValue); //this way I might notice when the bug gets fixed (https://github.com/dotnet/runtime/issues/29673)
    }
#endif

    [Fact]
    public void MemberMemberBindingForStructsFastCompilesOk()
    {
        var expr_compile_fast = Expr(() => new SomethingMutable { AStructField = { AValue = 2 } }).CompileFast()();
        Assert.Equal(2, expr_compile_fast.AStructField.AValue);
    }
}
