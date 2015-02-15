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
            new ExpressionToCodeImpl(
                (etp, depth) => {
                    sb.Append(ignoreInitialSpace ? etp.Text.TrimStart() : etp.Text);
                    ignoreInitialSpace = etp.Text.Any() && ShouldIgnoreSpaceAfter(etp.Text[etp.Text.Length - 1]);
                }).ExpressionDispatch(e);
            return sb.ToString();
        }

        public static string AnnotatedToCode(Expression expr) { return AnnotatedToCode(expr, null, false); }

        ///<summary>
        /// Converts expression to variable/property/method C# like representation adding it's string value.
        ///</summary>
        /// <example>
        /// string toNameValueRepresentation = "Value";
        /// ToRepr(() => toNameValueRepresentation); // "toNameValueRepresentation = Value"
        /// </example>
        /// <remarks>
        /// Unlike <see cref="ToCode"/>(which targets compilable output), this method is geared towards dumping simple objects into text, so may skip some C# issues for sake of readability.
        /// </remarks>
        public static string ToValuedCode<TResult>(this Expression<Func<TResult>> expression) {
            TResult retValue;
            try {
                retValue = expression.Compile().Invoke();
            } catch (Exception ex) {
                throw new InvalidOperationException("Cannon get return value of expression when it throws error", ex);
            }

            var name = ToFullName(expression);

            return string.Format("{0} = {1}", name, retValue);
        }

        internal static string AnnotatedToCode(Expression expr, string msg, bool ignoreOutermostValue) {
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

        //NOTE: should use recursive visitor as in other method when new failed test case added
        static string ToFullName<T>(this Expression<T> expression) {
            string name = null;
            var unaryExpression = expression.Body as UnaryExpression;
            if (unaryExpression != null) {
                name = unaryExpression.Operand.ToString().Split('.').Last();
                if (unaryExpression.NodeType == ExpressionType.ArrayLength) {
                    name += ".Length";
                }
            }
            var memberExpression = expression.Body as MemberExpression;
            if (memberExpression != null) {
                name = memberExpression.Member.Name;
            }
            var methodCallExpression = expression.Body as MethodCallExpression;
            if (methodCallExpression != null) {
                // tries transform method and return value in human readable C#-style representation
                var typePrefix = string.Empty;

                // add declaring type if it is not a module
                var arguments = String.Join(
                    ", ",
                    methodCallExpression.Arguments.Select(x => x.ToString()).ToArray() // converting to string to work for .NET 3.5 if backported
                    );
                var method = methodCallExpression.Method;
                var methodName = method.Name;
                if (method.IsGenericMethod) {
                    methodName += "<" + String.Join(
                        ", ",
                        method.GetGenericArguments().Select(x => x.Name).ToArray()) // converting to string to work for .NET 3.5 if backported
                        + ">";
                }
                if (methodName == "get_Item" && methodCallExpression.Arguments.Count > 0) //indexed property
                {
                    if (methodCallExpression.Object != null) {
                        typePrefix = methodCallExpression.Object.Type.Name;
                    }
                    name = String.Format("{0}[{1}]", typePrefix, arguments);
                } else {
                    if (method.IsStatic) {
                        typePrefix = method.DeclaringType.Name;
                    }
                    name = String.Format("{0}{1}{2}({3})", typePrefix, string.IsNullOrEmpty(typePrefix) ? "" : ".", methodName, arguments);
                }
            }
            if (name == null) {
                throw new ArgumentException("expression", "Failed to translate expression to its valued representation");
            }
            return name;
        }
    }
}
