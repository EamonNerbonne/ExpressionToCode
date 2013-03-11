using System;
using System.Collections.Generic;
using System.Linq;

namespace ExpressionToCodeLib {
	sealed class AssertFailedException : Exception {
		public AssertFailedException(string message) : base(message) { }
		public AssertFailedException(string message, Exception inner) : base(message, inner) { }
	}
}