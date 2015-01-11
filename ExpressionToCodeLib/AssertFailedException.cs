using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace ExpressionToCodeLib {
    [Obsolete(
        "This class is *not* the base class of all assertion violation exceptions - don't rely on it!  It will be removed in version 2."
        ), Serializable]
    public class PAssertFailedException : Exception {
        public PAssertFailedException(string message)
            : base(message) { }

        public PAssertFailedException(string message, Exception inner)
            : base(message, inner) { }

        public PAssertFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
#pragma warning disable 618
    [Serializable]
    sealed class AssertFailedException : PAssertFailedException
#pragma warning restore 618
    {
        public AssertFailedException(string message)
            : base(message) { }

        public AssertFailedException(string message, Exception inner)
            : base(message, inner) { }

        public AssertFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }

    namespace Internal {
        /// <summary>
        /// This class is not part of the public API: it's undocumented and minor version bumps may break compatiblity.
        /// </summary>
        public static class UnitTestingInternalsAccess {
            public static Exception CreateException(string msg) { return new AssertFailedException(msg); }
        }
    }
}
