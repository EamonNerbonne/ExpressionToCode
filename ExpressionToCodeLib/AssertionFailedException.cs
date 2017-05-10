using System;
using System.Collections.Generic;
using System.Linq;
#if dotnet_framework
using System.Runtime.Serialization;
#endif

namespace ExpressionToCodeLib
{
#if dotnet_framework
    [Serializable]
#endif
    sealed class AssertionFailedException : Exception
    {
        public AssertionFailedException(string message)
            : base(message) { }

        public AssertionFailedException(string message, Exception inner)
            : base(message, inner) { }

#if dotnet_framework
        // ReSharper disable once MemberCanBeProtected.Global
        public AssertionFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
#endif
    }
}
