using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExpressionToCodeLib;
using Xunit;

namespace ExpressionToCodeTest {
    public class FailingClass {
        public static int SomeFunction() { throw new Exception(); }
    }

    public class SubExprExceptionTest {
        [Fact]
        public void ExceptionDoesntCauseFailure() {
            Assert.Equal(
                @"() => FailingClass.SomeFunction()
                         │
                         throws System.Exception
",
                ExpressionToCode.AnnotatedToCode(() => FailingClass.SomeFunction()));
        }
    }
}
