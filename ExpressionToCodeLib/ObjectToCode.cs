using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ExpressionToCodeLib {
    public static class ObjectToCode {
        public static string ComplexObjectToPseudoCode(object val, int indent = 0) {
            string retval = PlainObjectToCode(val);
            if (retval != null) {
                return retval;
            } else if (val is Array) {
                return "new[] " + FormatEnumerable((IEnumerable)val);
            } else if (val is IEnumerable) {
                return FormatEnumerable((IEnumerable)val);
            } else if (val is Expression) {
                return ExpressionToCode.ToCode((Expression)val);
            } else if (val.GetType().GuessTypeClass() == ReflectionHelpers.TypeClass.AnonymousType) {
                var type = val.GetType();
                return "\n" + new string(' ', indent * 2) +
                    "new {" +
                    string.Join(
                        "",
                        type.GetProperties()
                            .Select(
                                pi =>
                                    "\n" + new string(' ', indent * 2 + 2) + pi.Name + " = "
                                        + ComplexObjectToPseudoCode(pi.GetValue(val, null), indent + 2) + ",")
                        )
                    + "\n" + new string(' ', indent * 2) + "}";
            } else {
                return val.ToString();
            }
        }

        public static string PlainObjectToCode(object val) { return PlainObjectToCode(val, val == null ? null : val.GetType()); }

        public static string PlainObjectToCode(object val, Type type) {
            if (val == null) {
                return type == null || type == typeof(object) ? "null" : "default(" + CSharpFriendlyTypeName.Get(type) + ")";
            } else if (val is string) {
                bool useLiteralSyntax = ((string)val).Any(c => c < 32 || c == '\\')
                    && ((string)val).All(c => c != '\n' && c != '\r' && c != '\t');
                if (useLiteralSyntax) {
                    return "@\"" + ((string)val).Replace("\"", "\"\"") + "\"";
                } else {
                    return "\"" + EscapeStringChars((string)val) + "\"";
                }
            } else if (val is char) {
                return "'" + EscapeStringChars(val.ToString()) + "'";
            } else if (val is decimal) {
                return Convert.ToString(val, CultureInfo.InvariantCulture) + "m";
            } else if (val is float) {
                return FloatToCode((float)val);
            } else if (val is double) {
                return DoubleToCode((double)val);
            } else if (val is byte || val is sbyte || val is short || val is ushort || val is int || val is uint || val is long
                || val is ulong) {
                return (Convert.ToString(val, CultureInfo.InvariantCulture)); //TODO: get numeric suffixes right - is this OK?
            } else if (val is bool && val.Equals(true)) {
                return "true";
            } else if (val is bool && val.Equals(false)) {
                return "false";
            } else if (val is Enum) {
                if (Enum.IsDefined(val.GetType(), val)) {
                    return val.GetType().Name + "." + val;
                } else {
                    long longVal = ((IConvertible)val).ToInt64(null);
                    var toString = ((IConvertible)val).ToString(CultureInfo.InvariantCulture);
                    if (toString == longVal.ToString(CultureInfo.InvariantCulture)) {
                        return "((" + val.GetType().Name + ")" + longVal + ")";
                    } else {
                        var components = toString.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                        return components.Length == 0
                            ? "default(" + val.GetType().Name + ")"
                            : components.Length == 1
                                ? val.GetType().Name + "." + components[0]
                                : "(" + string.Join(" | ", components.Select(s => val.GetType().Name + "." + s)) + ")";
                    }
                }
            } else if (val.GetType().IsValueType && Activator.CreateInstance(val.GetType()).Equals(val)) {
                return "default(" + CSharpFriendlyTypeName.Get(val.GetType()) + ")";
            } else if (val is Type) {
                return "typeof(" + CSharpFriendlyTypeName.Get((Type)val) + ")";
            } else if (val is MethodInfo) {
                return CSharpFriendlyTypeName.Get(((MethodInfo)val).DeclaringType) + "." + ((MethodInfo)val).Name;
            } else {
                return null;
            }
        }

        public static string GetCSharpFriendlyTypeName(Type type) { return CSharpFriendlyTypeName.Get(type); }

        static string EscapeCharForString(char c) {
            if (c < 32 || CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.Control) {
                //this is a little too rigorous; but easier to read 
                if (c == '\r') {
                    return "\\r";
                } else if (c == '\t') {
                    return "\\t";
                } else if (c == '\n') {
                    return "\\n";
                } else {
                    return "\\x" + Convert.ToString(c, 16);
                }
            } else if (c == '\\') {
                return @"\\";
            } else if (c == '\"') {
                return "\\\"";
            } else {
                return c.ToString();
            }
        }

        static string EscapeStringChars(string str) {
            StringBuilder sb = new StringBuilder(str.Length);
            foreach (char c in str) {
                sb.Append(EscapeCharForString(c));
            }
            return sb.ToString();
        }

        static string DoubleToCode(double p) {
            if (double.IsNaN(p)) {
                return "double.NaN";
            } else if (double.IsNegativeInfinity(p)) {
                return "double.NegativeInfinity";
            } else if (double.IsPositiveInfinity(p)) {
                return "double.PositiveInfinity";
            } else if (Math.Abs(p) > UInt32.MaxValue) {
                return p.ToString("0.0########################e0", CultureInfo.InvariantCulture);
            } else {
                return p.ToString("0.0########################", CultureInfo.InvariantCulture);
            }
        }

        static string FloatToCode(float p) {
            if (float.IsNaN(p)) {
                return "float.NaN";
            } else if (float.IsNegativeInfinity(p)) {
                return "float.NegativeInfinity";
            } else if (float.IsPositiveInfinity(p)) {
                return "float.PositiveInfinity";
            } else if (Math.Abs(p) >= (1 << 24)) {
                return p.ToString("0.0########e0", CultureInfo.InvariantCulture) + "f";
            } else {
                return p.ToString("0.0########", CultureInfo.InvariantCulture) + "f";
            }
        }

        static string FormatEnumerable(IEnumerable list) { return "{" + string.Join(", ", ExtractFirst10(list).ToArray()) + "}"; }

        static IEnumerable<string> ExtractFirst10(IEnumerable list) {
            int count = 0;
            foreach (var item in list) {
                count++;
                if (count > 10) {
                    yield return "...";
                    yield break;
                } else {
                    yield return ComplexObjectToPseudoCode(item);
                }
            }
        }
    }
}
