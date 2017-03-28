using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ExpressionToCodeLib.Internal
{
    static class ObjectToCodeImpl
    {
        public static string ComplexObjectToPseudoCode(ExpressionToCodeConfiguration config, object val, int indent)
            => ComplexObjectToPseudoCode(config, val, indent, config.Value.MaximumValueLength ?? int.MaxValue);

        internal static string ComplexObjectToPseudoCode(ExpressionToCodeConfiguration config, object val, int indent, int valueSize)
        {
            string retval = ObjectToCode.PlainObjectToCode(val);
            if (retval != null) {
                return ElideAfter(retval, valueSize);
            } else if (val is Array) {
                return "new[] " + FormatEnumerable(config, (IEnumerable)val, indent, valueSize - 6);
            } else if (val is IEnumerable) {
                return FormatEnumerable(config, (IEnumerable)val, indent, valueSize);
            } else if (val is Expression) {
                return ElideAfter(config.GetExpressionToCode().ToCode((Expression)val), valueSize);
            } else if (val.GetType().GuessTypeClass() == ReflectionHelpers.TypeClass.AnonymousType) {
                var type = val.GetType();
                return "new {" +
                    string.Join(
                        "",
                        type.GetTypeInfo().GetProperties()
                            .Select(
                                pi =>
                                    "\n" + new string(' ', indent * 2 + 2) + pi.Name + " = "
                                        + ComplexObjectToPseudoCode(config, pi.GetValue(val, null), indent + 2, valueSize - pi.Name.Length) + ",")
                        )
                    + "\n" + new string(' ', indent * 2) + "}";
            } else {
                return ElideAfter(val.ToString(), valueSize);
            }
        }

        static string ElideAfter(string val, int len)
        {
            var maxLength = Math.Max(10, len);
            return val.Length > maxLength ? val.Substring(0, maxLength) + " ..." : val;
        }

        static string FormatEnumerable(ExpressionToCodeConfiguration config, IEnumerable list, int indent, int valueSize)
        {
            var contents = PrintListContents(config, list, indent).ToArray();
            if (contents.Sum(s => s.Length + 2) > Math.Min(valueSize, 120) || contents.Any(s => s.Any(c => c == '\n'))) {
                var indentString = new string(' ', indent * 2 + 2);
                return "{\n"
                    + string.Join("", contents.Select(s => indentString + ElideAfter(s, valueSize - 3) + (s == "..." ? "" : ",") + "\n"))
                    + new string(' ', indent * 2)
                    + "}";
            }
            return "{" + string.Join(", ", contents) + "}";
        }

        static IEnumerable<string> PrintListContents(ExpressionToCodeConfiguration config, IEnumerable list, int indent)
        {
            int count = 0;
            foreach (var item in list) {
                count++;
                if (count > config.Value.PrintedListLengthLimit) {
                    yield return "...";
                    yield break;
                } else {
                    yield return ComplexObjectToPseudoCode(config, item, indent + 4);
                }
            }
        }

        public static string ExpressionValueAsCode(ExpressionToCodeConfiguration config, Expression expression)
        {
            try {
                Delegate lambda;
                try {
                    lambda = Expression.Lambda(expression).Compile();
                } catch (InvalidOperationException) {
                    return null;
                }

                var val = lambda.DynamicInvoke();
                try {
                    return ComplexObjectToPseudoCode(config, val, 0);
                } catch (Exception e) {
                    return "stringification throws " + e.GetType().FullName;
                }
            } catch (TargetInvocationException tie) {
                return "throws " + tie.InnerException.GetType().FullName;
            }
        }
    }
}
