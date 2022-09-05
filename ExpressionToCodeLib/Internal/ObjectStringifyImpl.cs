using System.Globalization;
using System.Text.RegularExpressions;

namespace ExpressionToCodeLib.Internal;

sealed class ObjectStringifyImpl : IObjectStringifier
{
    readonly bool fullTypeNames;
    readonly bool allowLiteralStrings;

    public ObjectStringifyImpl(bool fullTypeNames = false, bool allowVerbatimStrings = true)
    {
        this.fullTypeNames = fullTypeNames;
        this.allowVerbatimStrings = allowVerbatimStrings;
    }

    public string TypeNameToCode(Type type)
        => new CSharpFriendlyTypeName { UseFullName = fullTypeNames }.GetTypeName(type);

    public string? PlainObjectToCode(object? val, Type? type)
        => val switch {
            null when type == null || type == typeof(object) => "null",
            null => "default(" + TypeNameToCode(type) + ")",
            string str => UseVerbatimSyntax(str) ? "@\"" + str.Replace("\"", "\"\"") + "\"" : "\"" + EscapeStringChars(str) + "\"",
            char charVal => "'" + EscapeCharForString(charVal) + "'",
            decimal _ => Convert.ToString(val, CultureInfo.InvariantCulture) + "m",
            float floatVal => FloatToCode(floatVal),
            double doubleVal => DoubleToCode(doubleVal),
            byte byteVal => "((byte)" + byteVal + ")",
            sbyte sbyteVal => "((sbyte)" + sbyteVal + ")",
            short shortVal => "((short)" + shortVal + ")",
            ushort ushortVal => "((ushort)" + ushortVal + ")",
            int intVal => intVal.ToString(),
            uint uintVal => uintVal + "U",
            long longVal => longVal + "L",
            ulong ulongVal => ulongVal + "UL",
            bool boolVal => boolVal ? "true" : "false",
            Enum enumVal => EnumValueToCode(val, enumVal),
            Type typeVal => "typeof(" + TypeNameToCode(typeVal) + ")",
            MethodInfo methodInfoVal =>
                methodInfoVal.DeclaringType switch {
                    { } declaringType when declaringType.GuessTypeClass() is not (ReflectionHelpers.TypeClass.TopLevelProgramClosureType or ReflectionHelpers.TypeClass.ClosureType)
                        => TypeNameToCode(declaringType) + "." + methodInfoVal.Name,
                    _ when Regex.Match(methodInfoVal.Name, @"^.+>g__(\w+)\|[\d_]*$", RegexOptions.Multiline) is { Success: true } match =>
                        match.Groups[1].Value,
                    _ => methodInfoVal.Name,
                },
            _ when val is ValueType && (Activator.CreateInstance(val.GetType()) ?? throw new("value types cannot be null: " + val.GetType())).Equals(val) => "default(" + TypeNameToCode(val.GetType()) + ")",
            _ => null
        };

    string EnumValueToCode(object val, Enum enumVal)
    {
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
    }

    public bool UseVerbatimSyntax(string str)
    {
        if (!allowVerbatimStrings) {
            return false;
        }

        var count = 0;
        foreach (var c in str) {
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
        => c switch {
            //this is a little too rigorous; but easier to read
            '\r' => "\\r",
            '\t' => "\\t",
            '\n' => "\\n",
            '\\' => @"\\",
            '\"' => "\\\"",
            _ when c < 32 || CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.Control => "\\x" + Convert.ToString(c, 16),
            _ => c.ToString(),
        };

    internal static string EscapeStringChars(string str)
    {
        var sb = new StringBuilder(str.Length);
        foreach (var c in str) {
            _ = sb.Append(EscapeCharForString(c));
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
