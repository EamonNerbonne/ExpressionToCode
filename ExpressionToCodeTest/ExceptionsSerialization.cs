using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using ExpressionToCodeLib;
using ExpressionToCodeLib.Internal;
using NUnit.Framework;

namespace ExpressionToCodeTest {
	public class ExceptionsSerialization {
		[MethodImpl(MethodImplOptions.NoInlining)]
		static void IntentionallyFailingMethod() {
			PAssert.That(() => false);
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		static void IntentionallyFailingMethod2() {
			throw UnitTestingInternalsAccess.CreateException("Hello World!");
		}

		[Test]
		public void NUnitExceptionIsSerializable() {
			AssertMethodFailsWithSerializableException(IntentionallyFailingMethod);
		}
		[Test]
		public void PAssertExceptionIsSerializable() {
			AssertMethodFailsWithSerializableException(IntentionallyFailingMethod2);
		}

		static void AssertMethodFailsWithSerializableException(TestDelegate intentionallyFailingMethod) {
			var original = Assert.Catch<Exception>(intentionallyFailingMethod);

			var formatter = new BinaryFormatter();
			var ms = new MemoryStream();
			formatter.Serialize(ms, original);
			object deserialized = formatter.Deserialize(new MemoryStream(ms.ToArray()));

			Assert.AreEqual(original.ToString(), deserialized.ToString());
		}
	}
}
