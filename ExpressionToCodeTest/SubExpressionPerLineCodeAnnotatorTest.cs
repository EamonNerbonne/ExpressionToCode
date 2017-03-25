using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExpressionToCodeLib;
using Xunit;

namespace ExpressionToCodeTest
{
    public class SubExpressionPerLineCodeAnnotatorTest
    {
        static readonly ExpressionToCodeConfiguration config = ExpressionToCodeConfiguration.DefaultAssertionConfiguration.WithAnnotator(CodeAnnotators.SubExpressionPerLineCodeAnnotator);
        static readonly IAnnotatedToCode annotator = config.GetAnnotatedToCode();

        [Fact]
        public void AplusBapproved()
        {
            var a = 2;
            var b = 5;
            ApprovalTest.Verify(annotator.AnnotatedToCode(() => a + b));
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

        [Fact]
        public void Binary_expressions_with_nesting()
        {
            var a = 2;
            var b = 5;
            var c = 3.45;
            var nums = Enumerable.Range(10, 10).ToArray();
            ApprovalTest.Verify(annotator.AnnotatedToCode(() => a < b && (c > -a || c > b) && b < 10));
        }

        [Fact]
        public void MethodCallsAndArrayLiterals()
        {
            var a = 2;
            var b = 5;
            ApprovalTest.Verify(annotator.AnnotatedToCode(() => Math.Max(a, b) > new[] { 3, 8, 13, 4 }.Average()));
        }
    }
}
