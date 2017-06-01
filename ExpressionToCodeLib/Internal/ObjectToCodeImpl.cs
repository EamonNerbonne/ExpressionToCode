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
        static readonly string[] lineSeparators = new[] { "\r\n", "\n" };

        public static string ComplexObjectToPseudoCode(ExpressionToCodeConfiguration config, object val, int indent)
            => ComplexObjectToPseudoCode(config, val, indent, config.Value.MaximumValueLength ?? int.MaxValue);

        static string ComplexObjectToPseudoCode(ExpressionToCodeConfiguration config, object val, int indent, int valueSize)
        {
            var retval = ObjectToCode.PlainObjectToCode(val);
            if (val is string) {
                return ElidePossiblyMultilineString(config, retval, indent, valueSize).Trim();
            } else if (retval != null) {
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
                        type.GetTypeInfo()
                            .GetProperties()
                            .Select(
                                pi =>
                                    "\n" + new string(' ', indent + 4) + pi.Name + " = "
                                        + ComplexObjectToPseudoCode(config, pi.GetValue(val, null), indent + 4, valueSize - pi.Name.Length) + ",")
                        )
                    + "\n" + new string(' ', indent) + "}";
            } else {
                return ElideAfter(val.ToString(), valueSize);
            }
        }

        static string ElideAfter(string val, int len)
        {
            var maxLength = Math.Max(10, len);
            return val.Length > maxLength ? val.Substring(0, maxLength) + " ..." : val;
        }

        static string ElidePossiblyMultilineString(ExpressionToCodeConfiguration config, string val, int indent, int len)
        {
            var lines = val.Split(lineSeparators, StringSplitOptions.None);
            var indentString = new string(' ', indent);
            if (lines.Length < 2) {
                return "\n" + indentString + ElideAfter(val, len);
            }
            if (config.Value.PrintedListLengthLimit is int limit && lines.Length > limit) {
                lines = lines.Take(limit).Concat(new[] { "..." }).ToArray();
            }
            var stringBoundaryPrefix = lines[0].StartsWith("@\"") ? 2 : lines[0].StartsWith("\"") ? 1 : 0;
            var firstLineIndent = "\n" + indentString.Substring(0, Math.Max(0, indentString.Length - stringBoundaryPrefix));

            return firstLineIndent + string.Join("\n" + indentString, lines.Select(s => ElideAfter(s, len - 1)));
        }

        static string FormatEnumerable(ExpressionToCodeConfiguration config, IEnumerable list, int indent, int valueSize)
        {
            var contents = PrintListContents(config, list, indent).ToArray();
            if (contents.Sum(s => s.Length + 2) > Math.Min(valueSize, 120) || contents.Any(s => s.Any(c => c == '\n'))) {
                return "{"
                    + string.Join("", contents.Select(s => ElidePossiblyMultilineString(config, s, indent + 4, valueSize - 3) + (s == "..." ? "" : ",")))
                    + "\n" + new string(' ', indent)
                    + "}";
            }
            return "{" + string.Join(", ", contents) + "}";
        }

        static IEnumerable<string> PrintListContents(ExpressionToCodeConfiguration config, IEnumerable list, int indent)
        {
            var count = 0;
            foreach (var item in list) {
                count++;
                if (count > config.Value.PrintedListLengthLimit) {
                    yield return "...";
                    yield break;
                } else {
                    yield return ComplexObjectToPseudoCode(config, item, 0);
                }
            }
        }

        public static string ExpressionValueAsCode(ExpressionToCodeConfiguration config, Expression expression, int indent)
        {
            try {
                Delegate lambda;
                try {
                    lambda = config.Value.ExpressionCompiler.Compile(Expression.Lambda(expression));
                } catch (InvalidOperationException) {
                    return null;
                }

                var val = lambda.DynamicInvoke();
                try {
                    return ComplexObjectToPseudoCode(config, val, indent);
                } catch (Exception e) {
                    return "stringification throws " + e.GetType().FullName;
                }
            } catch (TargetInvocationException tie) {
                return "throws " + tie.InnerException.GetType().FullName;
            }
        }
    }
}
