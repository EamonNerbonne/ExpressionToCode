namespace ExpressionToCodeTest;

sealed class HasIndexers
{
    public object? this[string s] => null;
    public object? this[int i] => null;
}

public sealed class MultipleIndexerTest
{
    [Fact]
    public void CanPrettyPrintVariousIndexers()
        => Assert.Equal(
            "() => new HasIndexers()[3] == new HasIndexers()[\"three\"]",
            ExpressionToCode.ToCode(() => new HasIndexers()[3] == new HasIndexers()["three"])
        );
}
