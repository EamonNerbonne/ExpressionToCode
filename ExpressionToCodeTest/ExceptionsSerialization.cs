using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using ExpressionToCodeLib;
using ExpressionToCodeLib.Internal;
using Xunit;

namespace ExpressionToCodeTest
{
    public class ExceptionsSerialization
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void IntentionallyFailingMethod() { PAssert.That(() => false); }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void IntentionallyFailingMethod2() { throw UnitTestingInternalsAccess.CreateException("Hello World!"); }

        [Fact(Skip = "XUnit 2.10 exceptions don't appear to be serializable anymore... ?")]
        public void XUnitExceptionIsSerializable() { AssertMethodFailsWithSerializableException(IntentionallyFailingMethod); }

        [Fact]
        public void PAssertExceptionIsSerializable() { AssertMethodFailsWithSerializableException(IntentionallyFailingMethod2); }

        static void AssertMethodFailsWithSerializableException(Action intentionallyFailingMethod)
        {
            var original = Assert.ThrowsAny<Exception>(intentionallyFailingMethod);

            var formatter = new BinaryFormatter();
            var ms = new MemoryStream();
            formatter.Serialize(ms, original);
            object deserialized = formatter.Deserialize(new MemoryStream(ms.ToArray()));
            Assert.Equal(original.ToString(), deserialized.ToString());
        }
    }
}
