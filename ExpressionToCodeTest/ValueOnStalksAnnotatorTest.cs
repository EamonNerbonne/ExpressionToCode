using System;
using ExpressionToCodeLib;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;
using System.Linq.Expressions;

namespace ExpressionToCodeTest;

public class ValueOnStalksAnnotatorTest
{
    static readonly ExpressionToCodeConfiguration config =
        ExpressionToCodeConfiguration.DefaultAssertionConfiguration.WithAnnotator(CodeAnnotators.ValuesOnStalksCodeAnnotator);

    static readonly IAnnotatedToCode annotator = config.GetAnnotatedToCode();

    static string AnnotateAsAssertion<T>(Expression<Func<T>> expr) => annotator.AnnotatedToCode(expr.Body, "assertion failed", true);

    [Fact]
    public void AplusBapproved()
    {
        var a = 2;
        var b = 5;
        ApprovalTest.Verify(AnnotateAsAssertion(() => a + b > 3));
    }

    [Fact]
    public void Binary_expressions_with_nesting()
    {
        var a = 2;
        var b = 5;
        var c = 3.45;
        ApprovalTest.Verify(AnnotateAsAssertion(() => a < b && (c > -a || c > b) && b < 10));
    }

    [Fact]
    public void DealsOkWithLongEnumerables()
        => ApprovalTest.Verify(
            AnnotateAsAssertion(
                () => Enumerable.Range(0, 1000).ToDictionary(i => "n" + i)["n3"].ToString(CultureInfo.InvariantCulture) == 3.5.ToString(CultureInfo.InvariantCulture)
            ));

    [Fact]
    public void DealsOkWithLongStrings()
        => ApprovalTest.Verify(
            AnnotateAsAssertion(
                () => string.Join("##", Enumerable.Range(0, 100)) + "suffix"
            ));

    [Fact]
    public void DealsOkWithObjectsContainingLongStrings()
        => ApprovalTest.Verify(
            AnnotateAsAssertion(
                () => new {
                    A_long_string = string.Join("##", Enumerable.Range(0, 100)) + "suffix",
                    A_short_string = "short",
                    A_long_enumerable = Enumerable.Range(0, 1000)
                }
            ));

    [Fact]
    public void DealsOkWithEnumerablesOfAnonymousObjects()
    {
        var foo = new {
            A_long_string = string.Join("##", Enumerable.Range(0, 100)) + "suffix",
            A_short_string = "short",
            A_long_enumerable = Enumerable.Range(0, 1000)
        };
        ApprovalTest.Verify(
            AnnotateAsAssertion(
                () => new[] {
                    foo,
                    foo,
                }));
    }

    [Fact]
    public void DealsOkWithObjectsContainingLongMultilineStrings()
    {
        var wallOfText =
            string.Join(
                "",
                Enumerable.Range(0, 100)
                    .Select(
                        line =>
                            $"line {line}:".PadRight(10)
                            + string.Join(
                                "",
                                Enumerable.Range(2, 20).Select(n => $"{n * 10,9};")
                            ) + "\n"
                    )
            );

        ApprovalTest.Verify(
            AnnotateAsAssertion(
                () => new {
                    A_wall_of_text = wallOfText,
                    A_short_string = "short",
                    A_long_enumerable = Enumerable.Range(0, 1000)
                }
            ));
    }

    [Fact]
    public void MessyStructureElidesNeatly()
    {
        var hmm = "1234567890";
        ApprovalTest.Verify(
            AnnotateAsAssertion(
                // ReSharper disable once RedundantLogicalConditionalExpressionOperand
                () => hmm[1] == hmm[2] || hmm[4] == hmm[int.Parse(hmm[8].ToString())] || false
            ));
    }

    [Fact]
    public void MethodCallsAndArrayLiterals()
    {
        var a = 2;
        var b = 5;
        ApprovalTest.Verify(AnnotateAsAssertion(() => Math.Max(a, b) > new[] { 3, 8, 13, 4 }.Average()));
    }

    [Fact]
    public void NestedArrayAccess()
    {
        var a = 2;
        var b = 5;
        var nums = Enumerable.Range(10, 10).ToArray();
        ApprovalTest.Verify(AnnotateAsAssertion(() => nums[a + b] < 7));
    }

    [Fact]
    public void NestedArrayAccessWithOuterAnd()
    {
        var a = 2;
        var b = 5;
        var nums = Enumerable.Range(10, 10).ToArray();
        ApprovalTest.Verify(AnnotateAsAssertion(() => a < b && nums[a + b] < 7 && b < 10));
    }

    [Fact]
    public void NodesThatAreUndescribableAreNotDescribed()
    {
        var list = new List<int> { 1, 2, 3, 3, 2, 1 };
        ApprovalTest.Verify(AnnotateAsAssertion(() => list.Select(e => e + 1).Count() == 5));
    }
}