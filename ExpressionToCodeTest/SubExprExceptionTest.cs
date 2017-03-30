using System;
using System.Collections.Generic;
using System.Linq;
using ExpressionToCodeLib;
using Xunit;

namespace ExpressionToCodeTest
{
    public class FailingClass
    {
        public static int SomeFunction() => throw new Exception();
    }

    public class SubExprExceptionTest
    {
        [Fact]
        public void ExceptionDoesntCauseFailure()
        {
            Assert.Equal(
                @"() => FailingClass.SomeFunction()
FailingClass.SomeFunction()   →   throws System.Exception
".Replace("\r\n", "\n"),
                ExpressionToCode.AnnotatedToCode(() => FailingClass.SomeFunction()));
        }
    }
}
