using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExpressionToCodeLib;
using Xunit;

namespace ExpressionToCodeTest
{
    public class StringInterpolationTest
    {
        FormattableString Interpolation(FormattableString str) => str;

        [Fact]
        public void InterpolationWithArgumentsIsJustAString()
        {
            Assert.Equal(@"() => ""abc""",
                ExpressionToCode.ToCode(() => $"abc"));
        }

        [Fact]
        public void ForcedInterpolationWorks()
        {
            Assert.Equal(@"() => ""abc""",
                ExpressionToCode.ToCode(() => Interpolation($"abc")));
        }
    }
}
