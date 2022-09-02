using System;
using ExpressionToCodeLib;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ExpressionToCodeTest;

public class AnonymousObjectFormattingTest
{
    [Fact]
    public void AnonymousObjectsRenderAsCode()
        => Assert.Equal(
            @"new {
    A = 1,
    Foo = ""Bar"",
}",
            ObjectToCode.ComplexObjectToPseudoCode(new { A = 1, Foo = "Bar", }));

    [Fact]
    public void AnonymousObjectsInArray()
        => Assert.Equal(
            @"new[] {
    new {
        Val = 3,
    },
    new {
        Val = 42,
    },
}",
            ObjectToCode.ComplexObjectToPseudoCode(new[] { new { Val = 3, }, new { Val = 42 } }));

    [Fact]
    public void AnonymousObjectsInArrayExpression()
    {
        var arr = new[] { new { Name = "hmm", Val = (object)3, }, new { Name = "foo", Val = (object)"test" } };
        var config = ExpressionToCodeConfiguration.DefaultCodeGenConfiguration.WithAnnotator(CodeAnnotators.ValuesOnStalksCodeAnnotator);
        ApprovalTest.Verify(config.AnnotatedToCode(() => arr.Any()));
    }

    [Fact]
    public void MessyEnumerablesOfAnonymousObjects()
    {
        var foo = new {
            A_long_string = string.Join("##", Enumerable.Range(0, 100)) + "suffix",
            A_short_string = "short",
            A_long_enumerable = Enumerable.Range(0, 1000)
        };
        ApprovalTest.Verify(
            ExpressionToCodeConfiguration.DefaultAssertionConfiguration.ComplexObjectToPseudoCode(
                new[] {
                    foo,
                    foo,
                }));
    }

    [Fact]
    public void EnumerableInAnonymousObject()
        => Assert.Equal(
            @"new {
    Nums = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, ... },
}",
            ExpressionToCodeConfiguration.DefaultAssertionConfiguration.ComplexObjectToPseudoCode(new { Nums = Enumerable.Range(1, 100) }));

    [Fact]
    public void EnumInAnonymousObject()
        => Assert.Equal(
            @"new {
    Enum = ConsoleKey.A,
}",
            ObjectToCode.ComplexObjectToPseudoCode(new { Enum = ConsoleKey.A }));
}