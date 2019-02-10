using System;
using System.Globalization;
using System.Reflection;
using System.Linq;
using System.Text;

namespace ExpressionToCodeLib.Internal
{
    class ObjectStringifyImpl : IObjectStringifier
    {
        readonly bool fullTypeNames;

        public ObjectStringifyImpl(bool fullTypeNames = false)
            => this.fullTypeNames = fullTypeNames;

        public string TypeNameToCode(Type type)
            => new CSharpFriendlyTypeName { UseFullName = fullTypeNames }.GetTypeName(type);

        public string PlainObjectToCode(object val, Type type)
        {
            if (val == null) {
                return type == null || type == typeof(object) ? "null" : "default(" + TypeNameToCode(type) + ")";
            } else if (val is string str) {
                var useLiteralSyntax = PreferLiteralSyntax(str);
                if (useLiteralSyntax) {
                    return "@\"" + str.Replace("\"", "\"\"") + "\"";
                } else {
                    return "\"" + EscapeStringChars(str) + "\"";
                }
            } else if (val is char) {
                return "'" + EscapeStringChars(val.ToString()) + "'";
            } else if (val is decimal) {
                return Convert.ToString(val, CultureInfo.InvariantCulture) + "m";
            } else if (val is float floatVal) {
                return FloatToCode(floatVal);
            } else if (val is double doubleVal) {
                return DoubleToCode(doubleVal);
            } else if (val is byte) {
                return "((byte)" + Convert.ToString(val, CultureInfo.InvariantCulture) + ")";
            } else if (val is sbyte) {
                return "((sbyte)" + Convert.ToString(val, CultureInfo.InvariantCulture) + ")";
            } else if (val is short) {
                return "((short)" + Convert.ToString(val, CultureInfo.InvariantCulture) + ")";
            } else if (val is ushort) {
                return "((ushort)" + Convert.ToString(val, CultureInfo.InvariantCulture) + ")";
            } else if (val is int) {
                return Convert.ToString(val, CultureInfo.InvariantCulture);
            } else if (val is uint) {
                return Convert.ToString(val, CultureInfo.InvariantCulture) + "U";
            } else if (val is long) {
                return Convert.ToString(val, CultureInfo.InvariantCulture) + "L";
            } else if (val is ulong) {
                return Convert.ToString(val, CultureInfo.InvariantCulture) + "UL";
            } else if (val is bool && val.Equals(true)) {
                return "true";
            } else if (val is bool && val.Equals(false)) {
                return "false";
            } else if (val is Enum) {
                if (Enum.IsDefined(val.GetType(), val)) {
                    return TypeNameToCode(val.GetType()) + "." + val;
                } else {
                    var longVal = ((IConvertible)val).ToInt64(null);
                    var toString = ((IConvertible)val).ToString(CultureInfo.InvariantCulture);
                    if (toString == longVal.ToString(CultureInfo.InvariantCulture)) {
                        return "((" + TypeNameToCode(val.GetType()) + ")" + longVal + ")";
                    } else {
                        var components = toString.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                        return components.Length == 0
                            ? "default(" + TypeNameToCode(val.GetType()) + ")"
                            : components.Length == 1
                                ? TypeNameToCode(val.GetType()) + "." + components[0]
                                : "(" + string.Join(" | ", components.Select(s => TypeNameToCode(val.GetType()) + "." + s)) + ")";
                    }
                }
            } else if (val.GetType().GetTypeInfo().IsValueType && Activator.CreateInstance(val.GetType()).Equals(val)) {
                return "default(" + TypeNameToCode(val.GetType()) + ")";
            } else if (val is Type) {
                return "typeof(" + TypeNameToCode((Type)val) + ")";
            } else if (val is MethodInfo) {
                return TypeNameToCode(((MethodInfo)val).DeclaringType) + "." + ((MethodInfo)val).Name;
            } else {
                return null;
            }
        }

        internal static bool PreferLiteralSyntax(string str1)
        {
            var count = 0;
            foreach (var c in str1) {
                if (c < 32 && c != '\r' || c == '\\') {
                    count++;
                    if (count > 3) {
                        return true;
                    }
                }
            }

            return false;
        }

        static string EscapeCharForString(char c)
        {
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

        internal static string EscapeStringChars(string str)
        {
            var sb = new StringBuilder(str.Length);
            foreach (var c in str) {
                sb.Append(EscapeCharForString(c));
            }

            return sb.ToString();
        }

        static string DoubleToCode(double p)
        {
            if (double.IsNaN(p)) {
                return "double.NaN";
            } else if (double.IsNegativeInfinity(p)) {
                return "double.NegativeInfinity";
            } else if (double.IsPositiveInfinity(p)) {
                return "double.PositiveInfinity";
            } else if (Math.Abs(p) > uint.MaxValue) {
                return p.ToString("0.0########################e0", CultureInfo.InvariantCulture);
            } else {
                return p.ToString("0.0########################", CultureInfo.InvariantCulture);
            }
        }

        static string FloatToCode(float p)
        {
            if (float.IsNaN(p)) {
                return "float.NaN";
            } else if (float.IsNegativeInfinity(p)) {
                return "float.NegativeInfinity";
            } else if (float.IsPositiveInfinity(p)) {
                return "float.PositiveInfinity";
            } else if (Math.Abs(p) >= 1 << 24) {
                return p.ToString("0.0########e0", CultureInfo.InvariantCulture) + "f";
            } else {
                return p.ToString("0.0########", CultureInfo.InvariantCulture) + "f";
            }
        }
    }
}
