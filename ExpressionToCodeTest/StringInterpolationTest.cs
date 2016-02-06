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
        [Fact]
        public void BasicStringInterpolation()
        {
            Assert.Equal(@"() => $""abc""",
                ExpressionToCode.ToCode(() => $"abc"));
        }
    }
}
