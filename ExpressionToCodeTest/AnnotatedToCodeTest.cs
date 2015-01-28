using System;
using NUnit.Framework;

namespace ExpressionToCodeTest
{
    [TestFixture]
    public class AnnotatedToCodeTest
    {
        [Test]
        public void A1PlusB2()
        {
            var a = 1;
            var b = a + 1;
            
            var code = ExpressionToCodeLib.ExpressionToCode.AnnotatedToCode(()=> a + b);
            
            StringAssert.Contains("1",code);
            StringAssert.Contains("2",code);
            StringAssert.Contains("3",code);
        }
    }
}