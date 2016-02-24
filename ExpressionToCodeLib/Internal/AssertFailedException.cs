using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace ExpressionToCodeLib
{
    [Serializable]
    internal sealed class AssertFailedException : Exception
    {
        public AssertFailedException(string message)
            : base(message) { }

        public AssertFailedException(string message, Exception inner)
            : base(message, inner) { }

        public AssertFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }

    namespace Internal
    {
        /// <summary>
        /// This class is not part of the public API: it's undocumented and minor version bumps may break compatiblity.
        /// </summary>
        public static class UnitTestingInternalsAccess
        {
            public static Exception CreateException(string msg) => new AssertFailedException(msg);
        }
    }
}
