using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExpressionToCodeLib {
	public class PAssertFailedException : Exception {
		public PAssertFailedException(string message) : base(message) { }
	}
}
