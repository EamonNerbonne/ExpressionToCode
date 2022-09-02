using System;
using ExpressionToCodeLib;
using Xunit;

namespace ExpressionToCodeTest;

public class AnnotatedToCodeTest
{
    [Fact]
    public void A1PlusB2()
    {
        var a = 1;
        var b = a + 1;

        var code = ExpressionToCode.AnnotatedToCode(() => a + b);

        Assert.Contains("a", code);
        Assert.Contains("+", code);
        Assert.Contains("b", code);
        Assert.Contains("1", code);
        Assert.Contains("2", code);
        Assert.Contains("3", code);
    }
}