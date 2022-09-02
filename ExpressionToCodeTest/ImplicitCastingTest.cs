namespace ExpressionToCodeTest;

public class ImplicitCastingTest
{
    [Fact]
    public void CharNoCast()
        => Assert.Equal(
            @"() => ""abc""[1] == 'b'",
            ExpressionToCode.ToCode(() => "abc"[1] == 'b'));

    [Fact]
    public void CharComp()
    {
        var c = 'c';
        Assert.Equal(
            @"() => c == 'b'",
            ExpressionToCode.ToCode(() => c == 'b'));
    }

    [Fact]
    public void DecimalImplicitCast()
    {
        var i = 1;
        Assert.Equal(
            @"() => 1m + -i > 0m || false",
            // ReSharper disable once RedundantLogicalConditionalExpressionOperand
            ExpressionToCode.ToCode(() => 1m + -i > 0m || false));
    }

    [Fact]
    public void StringImplicitConcat()
    {
        var i = 1;
        var x = "X";
        Assert.Equal(
            @"() => 1m + x + ""!!"" + i",
            ExpressionToCode.ToCode(() => 1m + x + "!!" + i));
    }

    [Fact]
    public void NotImplicitCast()
    {
        byte z = 42;
        Assert.Equal(
            @"() => ~z == 0",
            ExpressionToCode.ToCode(() => ~z == 0));
    }

    [Fact]
    public void AvoidsImplicitBoxingWhenTargetTypeIsAGenericArgument()
        => Assert.Equal(
            @"() => StaticTestClass.TwoArgsTwoGeneric(3, new object())",
            ExpressionToCode.ToCode(() => StaticTestClass.TwoArgsTwoGeneric(3, new object()))
        );

    [Fact]
    public void AvoidsImplicitCastWhenTargetTypeIsAGenericArgument()
    {
        var x = 37;
        var y = 42.0;

        Assert.Equal(
            @"() => StaticTestClass.TwoArgsTwoGeneric(x, y)",
            ExpressionToCode.ToCode(() => StaticTestClass.TwoArgsTwoGeneric(x, y))
        );
    }
}
