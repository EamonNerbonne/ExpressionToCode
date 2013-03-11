using System;
using System.Collections.Generic;
using System.Linq;

namespace ExpressionToCodeLib {
	[Obsolete(
		"This class is *not* the base class of all assertion violation exceptions - don't rely on it!  It will be removed in version 2."
		)]
	public class PAssertFailedException : Exception {
		public PAssertFailedException(string message) : base(message) { }
		public PAssertFailedException(string message, Exception inner) : base(message, inner) { }
	}

#pragma warning disable 618
	sealed class AssertFailedException : PAssertFailedException
#pragma warning restore 618
	{
		public AssertFailedException(string message) : base(message) { }
		public AssertFailedException(string message, Exception inner) : base(message, inner) { }
	}
}