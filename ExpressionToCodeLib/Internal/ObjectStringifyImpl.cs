using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ExpressionToCodeLib.Internal
{
    internal class ObjectStringifyImpl : IObjectStringifier
    {
        readonly bool fullTypeNames;
        public ObjectStringifyImpl(bool fullTypeNames = false) { this.fullTypeNames = fullTypeNames; }
        public string TypeNameToCode(Type type) => new CSharpFriendlyTypeName { UseFullName = fullTypeNames }.GetTypeName(type);

        string IObjectStringifier.PlainObjectToCode(object val, Type type)
        {
            if (val == null) {
                return type == null || type == typeof(object) ? "null" : "default(" + TypeNameToCode(type) + ")";
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
                return Convert.ToString(val, CultureInfo.InvariantCulture); //TODO: get numeric suffixes right - is this OK?
            } else if (val is bool && val.Equals(true)) {
                return "true";
            } else if (val is bool && val.Equals(false)) {
                return "false";
            } else if (val is Enum) {
                if (Enum.IsDefined(val.GetType(), val)) {
                    return TypeNameToCode(val.GetType()) + "." + val;
                } else {
                    long longVal = ((IConvertible)val).ToInt64(null);
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

        static string EscapeStringChars(string str)
        {
            StringBuilder sb = new StringBuilder(str.Length);
            foreach (char c in str) {
                sb.Append(EscapeCharForString(c));
            }
            return sb.ToString();
        }

        static string DoubleToCode(double p)
        {
            if (Double.IsNaN(p)) {
                return "double.NaN";
            } else if (Double.IsNegativeInfinity(p)) {
                return "double.NegativeInfinity";
            } else if (Double.IsPositiveInfinity(p)) {
                return "double.PositiveInfinity";
            } else if (Math.Abs(p) > UInt32.MaxValue) {
                return p.ToString("0.0########################e0", CultureInfo.InvariantCulture);
            } else {
                return p.ToString("0.0########################", CultureInfo.InvariantCulture);
            }
        }

        static string FloatToCode(float p)
        {
            if (Single.IsNaN(p)) {
                return "float.NaN";
            } else if (Single.IsNegativeInfinity(p)) {
                return "float.NegativeInfinity";
            } else if (Single.IsPositiveInfinity(p)) {
                return "float.PositiveInfinity";
            } else if (Math.Abs(p) >= (1 << 24)) {
                return p.ToString("0.0########e0", CultureInfo.InvariantCulture) + "f";
            } else {
                return p.ToString("0.0########", CultureInfo.InvariantCulture) + "f";
            }
        }
    }
}