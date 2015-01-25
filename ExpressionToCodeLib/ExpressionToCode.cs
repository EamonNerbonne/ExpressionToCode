﻿using System;
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
		
		[Obsolete("Use ToAnnotatedCode")]
		public static string AnnotatedToCode<T, T1, T2, T3>(Expression<Func<T, T1, T2, T3>> e) { return ToAnnotatedCode((Expression)e); }
		[Obsolete("Use ToAnnotatedCode")]
		public static string AnnotatedToCode<T, T1, T2>(Expression<Func<T, T1, T2>> e) { return ToAnnotatedCode((Expression)e); }
		[Obsolete("Use ToAnnotatedCode")]
		public static string AnnotatedToCode<T, T1>(Expression<Func<T, T1>> e) { return ToAnnotatedCode((Expression)e); }
		[Obsolete("Use ToAnnotatedCode")]
		public static string AnnotatedToCode<T>(Expression<Func<T>> e) { return ToAnnotatedCode((Expression)e); }

		public static string ToAnnotatedCode<T, T1, T2, T3>(Expression<Func<T, T1, T2, T3>> e) { return ToAnnotatedCode((Expression)e); }
		public static string ToAnnotatedCode<T, T1, T2>(Expression<Func<T, T1, T2>> e) { return ToAnnotatedCode((Expression)e); }
		public static string ToAnnotatedCode<T, T1>(Expression<Func<T, T1>> e) { return ToAnnotatedCode((Expression)e); }
		public static string ToAnnotatedCode<T>(Expression<Func<T>> e) { return ToAnnotatedCode((Expression)e); }
		
		public static string ToCode(Expression e) {
			StringBuilder sb = new StringBuilder();
			bool ignoreInitialSpace = true;
			new ExpressionToCodeImpl(
				(etp, depth) => {
					sb.Append(ignoreInitialSpace ? etp.Text.TrimStart() : etp.Text);
					ignoreInitialSpace = etp.Text.Any() && ShouldIgnoreSpaceAfter(etp.Text[etp.Text.Length - 1]);
				}).ExpressionDispatch(e);
			return sb.ToString();
		}
		
		[Obsolete("Use ToAnnotatedCode")]
		public static string AnnotatedToCode(Expression expr) { return ToAnnotatedCode(expr, null, false); }
		public static string ToAnnotatedCode(Expression expr) { return ToAnnotatedCode(expr, null, false); }
		
		internal static string ToAnnotatedCode(Expression expr, string msg, bool ignoreOutermostValue) {
			var splitLine = ExpressionToStringWithValues(expr, ignoreOutermostValue);

			var exprWithStalkedValues = new StringBuilder();
			if (msg == null) {
				exprWithStalkedValues.AppendLine(splitLine.Line);
			} else if (IsMultiline(msg)) {
				exprWithStalkedValues.AppendLine(msg);
				exprWithStalkedValues.AppendLine(splitLine.Line);
			} else {
				exprWithStalkedValues.AppendLine(splitLine.Line + "  :  " + msg);
			}

			for (int nodeI = splitLine.Nodes.Length - 1; nodeI >= 0; nodeI--) {
				char[] stalkLine = new string('\u2007', splitLine.Nodes[nodeI].Location).ToCharArray(); //figure-spaces.
				for (int i = 0; i < stalkLine.Length; i++) {
					if (splitLine.Line[i] == ' ') {
						stalkLine[i] = ' '; //use normal spaces where the expr used normal spaces for more natural spacing.
					}
				}

				for (int prevI = 0; prevI < nodeI; prevI++) {
					stalkLine[splitLine.Nodes[prevI].Location] = '\u2502'; //light vertical lines
				}
				exprWithStalkedValues.AppendLine((new string(stalkLine) + splitLine.Nodes[nodeI].Value).TrimEnd());
			}

			return exprWithStalkedValues.ToString();
		}

		static bool IsMultiline(string msg) {
			var idxAfterNewline = msg.IndexOf('\n') + 1;
			return idxAfterNewline > 0 && idxAfterNewline < msg.Length;
		}

		static bool ShouldIgnoreSpaceAfter(char c) { return c == ' ' || c == '('; }

		static SplitExpressionLine ExpressionToStringWithValues(Expression e, bool ignoreOutermostValue) {
			var nodeInfos = new List<SubExpressionInfo>();
			StringBuilder sb = new StringBuilder();
			bool ignoreInitialSpace = true;
			new ExpressionToCodeImpl(
				(etp, depth) => {
					var trimmedText = ignoreInitialSpace ? etp.Text.TrimStart() : etp.Text;
					var pos0 = sb.Length;
					sb.Append(trimmedText);
					ignoreInitialSpace = etp.Text.Any() && ShouldIgnoreSpaceAfter(etp.Text[etp.Text.Length - 1]);
					if (depth == 0 && ignoreOutermostValue) {
						return;
					}
					string valueString = etp.OptionalValue == null ? null : ExpressionValueAsCode(etp.OptionalValue);
					if (valueString != null) {
						nodeInfos.Add(new SubExpressionInfo { Location = pos0 + trimmedText.Length / 2, Value = valueString });
					}
				}).ExpressionDispatch(e);
			nodeInfos.Add(new SubExpressionInfo { Location = sb.Length, Value = null });
			return new SplitExpressionLine { Line = sb.ToString().TrimEnd(), Nodes = nodeInfos.ToArray() };
		}

		static string ExpressionValueAsCode(Expression expression) {
			try {
				Delegate lambda;
				try {
					lambda = Expression.Lambda(expression).Compile();
				} catch (InvalidOperationException) {
					return null;
				}

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
