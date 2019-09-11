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
                return PreferLiteralSyntax(str) ? "@\"" + str.Replace("\"", "\"\"") + "\"" : "\"" + EscapeStringChars(str) + "\"";
            } else if (val is char charVal) {
                return "'" + EscapeCharForString(charVal) + "'";
            } else if (val is decimal) {
                return Convert.ToString(val, CultureInfo.InvariantCulture) + "m";
            } else if (val is float floatVal) {
                return FloatToCode(floatVal);
            } else if (val is double doubleVal) {
                return DoubleToCode(doubleVal);
            } else if (val is byte byteVal) {
                return "((byte)" + byteVal + ")";
            } else if (val is sbyte sbyteVal) {
                return "((sbyte)" + sbyteVal + ")";
            } else if (val is short shortVal) {
                return "((short)" + shortVal + ")";
            } else if (val is ushort ushortVal) {
                return "((ushort)" + ushortVal + ")";
            } else if (val is int intVal) {
                return intVal.ToString();
            } else if (val is uint uintVal) {
                return uintVal + "U";
            } else if (val is long longVal) {
                return longVal + "L";
            } else if (val is ulong ulongVal) {
                return ulongVal + "UL";
            } else if (val is bool boolVal) {
                return boolVal ? "true" : "false";
            } else if (val is Enum enumVal) {
                if (Enum.IsDefined(enumVal.GetType(), enumVal)) {
                    return TypeNameToCode(enumVal.GetType()) + "." + enumVal;
                } else {
                    var enumAsLong = ((IConvertible)enumVal).ToInt64(null);
                    var toString = enumVal.ToString();
                    if (toString == enumAsLong.ToString()) {
                        return "((" + TypeNameToCode(enumVal.GetType()) + ")" + enumAsLong + ")";
                    } else {
                        var components = toString.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                        return components.Length == 0
                            ? "default(" + TypeNameToCode(enumVal.GetType()) + ")"
                            : components.Length == 1
                                ? TypeNameToCode(enumVal.GetType()) + "." + components[0]
                                : "(" + string.Join(" | ", components.Select(s => TypeNameToCode(val.GetType()) + "." + s)) + ")";
                    }
                }
            } else if (val.GetType().GetTypeInfo().IsValueType && Activator.CreateInstance(val.GetType()).Equals(val)) {
                return "default(" + TypeNameToCode(val.GetType()) + ")";
            } else if (val is Type typeVal) {
                return "typeof(" + TypeNameToCode(typeVal) + ")";
            } else if (val is MethodInfo methodInfoVal) {
                return TypeNameToCode(methodInfoVal.DeclaringType) + "." + methodInfoVal.Name;
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
