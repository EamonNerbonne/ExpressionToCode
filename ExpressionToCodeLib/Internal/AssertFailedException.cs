using System;
using System.Collections.Generic;
using System.Linq;
#if dotnet_framework
using System.Runtime.Serialization;
#endif

namespace ExpressionToCodeLib.Internal
{
#if dotnet_framework
    [Serializable]
#endif
    sealed class AssertFailedException : Exception
    {
        public AssertFailedException(string message)
            : base(message) { }

        public AssertFailedException(string message, Exception inner)
            : base(message, inner) { }

#if dotnet_framework
        // ReSharper disable once MemberCanBeProtected.Global
        public AssertFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
#endif
    }

    namespace Internal
    {
        /// <summary>
        /// This class is not part of the public API: it's undocumented and minor version bumps may break compatiblity.
        /// </summary>
        public static class UnitTestingInternalsAccess
        {
            public static Exception CreateException(string msg)
                => new AssertFailedException(msg);
        }
    }
}
