using System;
using System.Collections.Generic;
using System.Linq;

namespace ExpressionToCodeLib {
	public sealed class PAssertFailedException : Exception {
		public PAssertFailedException(string message) : base(message) { }
		public PAssertFailedException(string message, Exception inner) : base(message, inner) { }
	}
}