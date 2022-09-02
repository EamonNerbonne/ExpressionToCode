﻿using System.Globalization;
using ExpressionToCodeLib;
using System.Linq;
using Xunit;

namespace ExpressionToCodeTest;

public class EnumerableFormattingTest
{
    [Fact]
    public void ShortArraysRenderedCompletely()
        => Assert.Equal("new[] { 1, 2, 3 }", ObjectToCode.ComplexObjectToPseudoCode(new[] { 1, 2, 3 }));

    [Fact]
    public void ShortEnumerablesRenderedCompletely()
        => Assert.Equal("{ 1, 2, 3 }", ObjectToCode.ComplexObjectToPseudoCode(Enumerable.Range(1, 3)));

    [Fact]
    public void LongEnumerablesBreakAfter10_InCodeGen()
        => Assert.Equal(
            "{ 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 }",
            ExpressionToCodeConfiguration.DefaultCodeGenConfiguration.ComplexObjectToPseudoCode(Enumerable.Range(1, 18)));

    [Fact]
    public void LongArraysBreakAfter30_InAssertions()
        => Assert.Equal(
            "new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, ... }",
            ExpressionToCodeConfiguration.DefaultAssertionConfiguration.ComplexObjectToPseudoCode(Enumerable.Range(1, 31).ToArray()));

    [Fact]
    public void LongArraysDoNotBreakAfter30_InCodeGen()
        => Assert.Equal("new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32 }", ObjectToCode.ComplexObjectToPseudoCode(Enumerable.Range(1, 32).ToArray()));

    [Fact]
    public void LongArraysDoNotBreakIfSoConfigured()
    {
        var config = ExpressionToCodeConfiguration.DefaultCodeGenConfiguration.WithPrintedListLengthLimit(null);
        Assert.Equal("new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 }", config.ComplexObjectToPseudoCode(Enumerable.Range(1, 13).ToArray()));
    }

    [Fact]
    public void EnumerableElisionIsConfigurable()
    {
        var config = ExpressionToCodeConfiguration.DefaultCodeGenConfiguration.WithPrintedListLengthLimit(3);
        Assert.Equal("{ 1, 2, 3, ... }", config.ComplexObjectToPseudoCode(Enumerable.Range(1, 13)));
    }

    [Fact]
    public void InsertsLineBreaksWhenNecessary()
        => Assert.Equal(
            @"{
    ""12345000"",
    ""12345001"",
    ""12345002"",
    ""12345003"",
    ""12345004"",
    ""12345005"",
    ""12345006"",
    ""12345007"",
    ""12345008"",
    ""12345009"",
    ...
}".Replace("\r", ""),
            ExpressionToCodeConfiguration.DefaultAssertionConfiguration.WithPrintedListLengthLimit(10).ComplexObjectToPseudoCode(
                Enumerable.Range(12345000, 13).Select(i => i.ToString(CultureInfo.InvariantCulture))));

    [Fact]
    public void InsertsLineBreaksContentsContainLineBreaks()
        => Assert.Equal(
            @"{
    new {
        A = 3,
        B = 'b',
    },
    new {
        A = 3,
        B = 'b',
    },
}".Replace("\r", ""),
            ObjectToCode.ComplexObjectToPseudoCode(Enumerable.Repeat(new { A = 3, B = 'b' }, 2)));

    [Fact]
    public void NestedArraysUseProperConfig()
    {
        var config = ExpressionToCodeConfiguration.DefaultCodeGenConfiguration.WithPrintedListLengthLimit(3);
        ApprovalTest.Verify(
            config.ComplexObjectToPseudoCode(
                new[] {
                    null,
                    new {
                        A = 3,
                        B = new[] { 1, 2, 3, 4, 5 }
                    }
                }));
    }
}