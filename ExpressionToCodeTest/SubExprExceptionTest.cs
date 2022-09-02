namespace ExpressionToCodeTest;

public class FailingClass
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool SomeFunction()
        => throw new Exception();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool SomeWrappedFunction()
        => SomeFunction();
}

public class SubExprExceptionTest
{
    [Fact]
    public void ExceptionDoesntCauseFailure()
        => Assert.Equal(
            @"() => FailingClass.SomeWrappedFunction()
FailingClass.SomeWrappedFunction()
     →   throws System.Exception

".Replace("\r\n", "\n"),
            ExpressionToCode.AnnotatedToCode(() => FailingClass.SomeWrappedFunction()));
}
