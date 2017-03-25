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
    }
}
