using System;
using System.IO;
using System.Runtime.CompilerServices;
using ExpressionToCodeLib;
using Xunit;
//requires binary serialization, which is omitted in older .net cores - but those are out of support: https://docs.microsoft.com/en-us/lifecycle/products/microsoft-net-and-net-core

namespace ExpressionToCodeTest
{
    public class ExceptionsSerialization
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void IntentionallyFailingMethod()
            => PAssert.That(() => false);

        [Fact]
        public void PAssertExceptionIsSerializable()
            => AssertMethodFailsWithSerializableException(IntentionallyFailingMethod);

#pragma warning disable SYSLIB0011 // BinaryFormatter is Obsolete
        static void AssertMethodFailsWithSerializableException(Action intentionallyFailingMethod)
        {
            var original = Assert.ThrowsAny<Exception>(intentionallyFailingMethod);
            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            var ms = new MemoryStream();
            formatter.Serialize(ms, original);
            var deserialized = formatter.Deserialize(new MemoryStream(ms.ToArray()));
            Assert.Equal(original.ToString(), deserialized.ToString());
        }
#pragma warning restore SYSLIB0011
    }
}
