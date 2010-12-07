using System;
using System.Linq.Expressions;

namespace ExpressionToCodeLib {
	class ExprTextPart {
		public readonly string Text;
		public readonly Expression OptionalValue;
		ExprTextPart(string text, Expression val) {
			if (text == null) throw new ArgumentNullException("text");
			Text = text; OptionalValue = val;
		}
		public static ExprTextPart TextOnly(string text) { return new ExprTextPart(text, null); }
		public static ExprTextPart TextAndExpr(string text, Expression expr) {
			if (expr == null) throw new ArgumentNullException("expr");
			return new ExprTextPart(text, expr);
		}
	}
}
