using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ExpressionToCodeLib {
	public static class ExpressionToCode {
		public static string ToCode<T, T1, T2, T3>(Expression<Func<T, T1, T2, T3>> e) { return ToCode((Expression)e); }
		public static string ToCode<T, T1, T2>(Expression<Func<T, T1, T2>> e) { return ToCode((Expression)e); }
		public static string ToCode<T, T1>(Expression<Func<T, T1>> e) { return ToCode((Expression)e); }
		public static string ToCode<T>(Expression<Func<T>> e) { return ToCode((Expression)e); }
		public static string AnnotatedToCode<T, T1, T2, T3>(Expression<Func<T, T1, T2, T3>> e) { return AnnotatedToCode((Expression)e); }
		public static string AnnotatedToCode<T, T1, T2>(Expression<Func<T, T1, T2>> e) { return AnnotatedToCode((Expression)e); }
		public static string AnnotatedToCode<T, T1>(Expression<Func<T, T1>> e) { return AnnotatedToCode((Expression)e); }
		public static string AnnotatedToCode<T>(Expression<Func<T>> e) { return AnnotatedToCode((Expression)e); }

		public static string ToCode(Expression e) {
			StringBuilder sb = new StringBuilder();
			bool ignoreInitialSpace = true;
			new ExpressionToCodeImpl(etp => {
				sb.Append(ignoreInitialSpace ? etp.Text.TrimStart() : etp.Text);
				ignoreInitialSpace = etp.Text.Any() && ShouldIgnoreSpaceAfter(etp.Text[etp.Text.Length - 1]);
			}).ExpressionDispatch(e);
			return sb.ToString();
		}

		public static string AnnotatedToCode(Expression expr) {
			var splitLine = ExpressionToStringWithValues(expr);

			StringBuilder exprWithStalkedValues = new StringBuilder();
			exprWithStalkedValues.AppendLine(splitLine.Line);
			for (int nodeI = splitLine.Nodes.Length - 1; nodeI >= 0; nodeI--) {
				char[] stalkLine = new string(' ', splitLine.Nodes[nodeI].Location).ToCharArray();
				for (int prevI = 0; prevI < nodeI; prevI++)
					stalkLine[splitLine.Nodes[prevI].Location] = '|';
				exprWithStalkedValues.AppendLine((new string(stalkLine) + splitLine.Nodes[nodeI].Value).TrimEnd());
			}

			return exprWithStalkedValues.ToString();
		}


		static bool ShouldIgnoreSpaceAfter(char c) { return c == ' ' || c == '('; }

		static SplitExpressionLine ExpressionToStringWithValues(Expression e) {
			var nodeInfos = new List<SubExpressionInfo>();
			StringBuilder sb = new StringBuilder();
			bool ignoreInitialSpace = true;
			new ExpressionToCodeImpl(etp => {
				var trimmedText = ignoreInitialSpace ? etp.Text.TrimStart() : etp.Text;
				var pos0 = sb.Length;
				sb.Append(trimmedText);
				ignoreInitialSpace = etp.Text.Any() && ShouldIgnoreSpaceAfter(etp.Text[etp.Text.Length - 1]);
				string valueString = etp.OptionalValue == null ? null : ExpressionValueAsCode(etp.OptionalValue);
				if (valueString != null)
					nodeInfos.Add(new SubExpressionInfo { Location = pos0 + trimmedText.Length / 2, Value = valueString });
			}).ExpressionDispatch(e);
			nodeInfos.Add(new SubExpressionInfo { Location = sb.Length, Value = null });
			return new SplitExpressionLine { Line = sb.ToString().TrimEnd(), Nodes = nodeInfos.ToArray() };
		}

		static string ExpressionValueAsCode(Expression expression) {
			try {
				Delegate lambda;
				try {
					lambda = Expression.Lambda(expression).Compile();
				} catch (InvalidOperationException) { return null; }

				var val = lambda.DynamicInvoke();
				try {
					return ObjectToCode.ComplexObjectToPseudoCode(val);
				} catch (Exception e) {
					return "stringification throws " + e.GetType().FullName;
				}
			} catch (TargetInvocationException tie) {
				return "throws " + tie.InnerException.GetType().FullName;
			}
		}
		struct SplitExpressionLine {
			public string Line;
			public SubExpressionInfo[] Nodes;
		}
		struct SubExpressionInfo {
			public int Location;
			public string Value;
		}
	}
}
