using System;
using ExpressionToCodeLib;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ExpressionToCodeTest
{
    public class SubExpressionPerLineCodeAnnotatorTest
    {
        static readonly ExpressionToCodeConfiguration config =
            ExpressionToCodeConfiguration.DefaultAssertionConfiguration.WithAnnotator(CodeAnnotators.SubExpressionPerLineCodeAnnotator);

        static readonly IAnnotatedToCode annotator = config.GetAnnotatedToCode();

        [Fact]
        public void AplusBapproved()
        {
            var a = 2;
            var b = 5;
            ApprovalTest.Verify(annotator.AnnotatedToCode(() => a + b));
        }

        [Fact]
        public void Binary_expressions_with_nesting()
        {
            var a = 2;
            var b = 5;
            var c = 3.45;
            ApprovalTest.Verify(annotator.AnnotatedToCode(() => a < b && (c > -a || c > b) && b < 10));
        }

        [Fact]
        public void DealsOkWithLongEnumerables()
        {
            ApprovalTest.Verify(
                annotator.AnnotatedToCode(
                    () => Enumerable.Range(0, 1000).ToDictionary(i => "n" + i)["n3"].ToString() == 3.5.ToString()
                ));
        }

        [Fact]
        public void DealsOkWithLongStrings()
        {
            ApprovalTest.Verify(
                annotator.AnnotatedToCode(
                    () => string.Join("##", Enumerable.Range(0, 100)) + "suffix"
                ));
        }

        [Fact]
        public void DealsOkWithObjectsContainingLongStrings()
        {
            ApprovalTest.Verify(
                annotator.AnnotatedToCode(
                    () => new {
                        A_long_string = string.Join("##", Enumerable.Range(0, 100)) + "suffix",
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
                annotator.AnnotatedToCode(
                    () => hmm[1] == hmm[2] || hmm[4] == hmm[int.Parse(hmm[8].ToString())] || false
                ));
        }

        [Fact]
        public void MethodCallsAndArrayLiterals()
        {
            var a = 2;
            var b = 5;
            ApprovalTest.Verify(annotator.AnnotatedToCode(() => Math.Max(a, b) > new[] { 3, 8, 13, 4 }.Average()));
        }

        [Fact]
        public void NestedArrayAccess()
        {
            var a = 2;
            var b = 5;
            var nums = Enumerable.Range(10, 10).ToArray();
            ApprovalTest.Verify(annotator.AnnotatedToCode(() => nums[a + b] < 7));
        }

        [Fact]
        public void NestedArrayAccessWithOuterAnd()
        {
            var a = 2;
            var b = 5;
            var nums = Enumerable.Range(10, 10).ToArray();
            ApprovalTest.Verify(annotator.AnnotatedToCode(() => a < b && nums[a + b] < 7 && b < 10));
        }
    }
}
