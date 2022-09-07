using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ExpressionToCodeLib.Internal;

static class ObjectToCodeImpl
{
    static readonly string[] lineSeparators = { "\r\n", "\n", };

    public static string ComplexObjectToPseudoCode(ExpressionToCodeConfiguration config, object? val, int indent)
        => ComplexObjectToPseudoCode(config, val, indent, config.MaximumValueLength ?? int.MaxValue);

    static string ComplexObjectToPseudoCode(ExpressionToCodeConfiguration config, object? val, int indent, int valueSize)
    {
        var retval = ObjectToCode.PlainObjectToCode(val);
        if (val is string) {
            return ElidePossiblyMultilineString(config, retval ?? throw new InvalidOperationException("retval cannot be null for strings"), indent, valueSize).Trim();
        } else if (retval != null) {
            return ElideAfter(retval, valueSize);
        } else if (val is null) {
            throw new("Impossible: if val is null, retval cannot be");
        } else if (val is Array arrayVal) {
            return "new[] " + FormatEnumerable(config, arrayVal, indent, valueSize - 6);
        } else if (val is IEnumerable enumerableVal) {
            return FormatTypeWithListInitializerOrNull(config, val, indent, valueSize, enumerableVal) ?? FormatEnumerable(config, enumerableVal, indent, valueSize);
        } else if (val is Expression exprVal) {
            return ElideAfter(config.GetExpressionToCode().ToCode(exprVal), valueSize);
        } else if (val is IStructuralComparable tuple and IComparable && TypeToCodeConfig.IsValueTupleType(val.GetType().GetTypeInfo())) {
            var collector = new NastyHackyTupleCollector();
            _ = tuple.CompareTo(tuple, collector); //ignore return value; we're abusing the implementation of equality to help us enumerate its contents
            var sb = new StringBuilder();
            _ = sb.Append("(");
            for (var index = 0; index < collector.CollectedObjects.Count; index++) {
                var item = collector.CollectedObjects[index];
                var asString = ComplexObjectToPseudoCode(config, item, indent + 4, valueSize);
                if (index > 0) {
                    _ = sb.Append(", ");
                }

                _ = sb.Append(asString);
            }

            _ = sb.Append(")");
            return ElidePossiblyMultilineString(config, sb.ToString(), indent, valueSize).Trim();
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
            return ElideAfter(val.ToString() ?? "", valueSize);
        }
    }

    static string? FormatTypeWithListInitializerOrNull(ExpressionToCodeConfiguration config, object val, int indent, int valueSize, IEnumerable enumerableVal)
    {
        var type = val.GetType();
        if (!(type.GetConstructor(Type.EmptyTypes) is { } ci) || !ci.IsPublic) {
            return null;
        }

        foreach (var pi in type.GetProperties()) {
            if (
                pi.Name == "Item"
                && pi.CanWrite
                && pi.GetIndexParameters() is { } indexPars
                && indexPars.Length == 1
                && typeof(IEnumerable<>).MakeGenericType(typeof(KeyValuePair<,>).MakeGenericType(indexPars[0].ParameterType, pi.PropertyType)) is { } keyEnumerableType
                && keyEnumerableType.IsAssignableFrom(type)
            ) {
                var typeName = type.ToCSharpFriendlyTypeName();
                return "new " + typeName + " " + PrintInitializerContents(config, enumerableVal, indexPars[0].ParameterType, pi.PropertyType, indent, valueSize - typeName.Length);
            }
        }

        return null;
    }

    sealed class NastyHackyTupleCollector : IComparer
    {
        //hack assumptions:
        // - A structural ordering comparer must in some way iterate over its contents.
        // - a tuple is defined to consider the order of its earlier members over that of its later members
        // - if earlier members are equal in order (compare==0), then it must call later members
        // - if everything is equal, it must call compareto on everything
        // - it would be inefficient to call compareto unnecessarily, so any tuple implementation is *likely* to call compareto in tuple-member-order, so it can exit early on non-zero comparison.
        public readonly List<object?> CollectedObjects = new();
        int nesting = 1;

        public int Compare(object? x, object? y)
        {
            if (CollectedObjects.Count == nesting * 7 && x is IStructuralComparable tuple && tuple is IComparable && TypeToCodeConfig.IsValueTupleType(tuple.GetType().GetTypeInfo())) {
                nesting++;
                return tuple.CompareTo(tuple, this);
            }

            CollectedObjects.Add(x);
            return 0;
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

        if (config.PrintedListLengthLimit is { } limit && lines.Length > limit) {
            lines = lines.Take(limit).Concat(new[] { "...", }).ToArray();
        }

        var stringBoundaryPrefix = lines[0].StartsWith("@\"", StringComparison.Ordinal) ? 2 : lines[0].StartsWith("\"", StringComparison.Ordinal) ? 1 : 0;
        var firstLineIndent = "\n" + indentString.Substring(0, Math.Max(0, indentString.Length - stringBoundaryPrefix));

        return firstLineIndent + string.Join("\n" + indentString, lines.Select(s => ElideAfter(s, len - 1)));
    }

    static string FormatEnumerable(ExpressionToCodeConfiguration config, IEnumerable list, int indent, int valueSize)
        => FormatLiteralInitializerContents(config, PrintListContents(config, list), indent, valueSize);

    static string FormatLiteralInitializerContents(ExpressionToCodeConfiguration config, IEnumerable<string> contents, int indent, int valueSize)
    {
        if (contents.Sum(s => s.Length + 2) > Math.Min(valueSize, 120) || contents.Any(s => s.Any(c => c == '\n'))) {
            return "{"
                + string.Join("", contents.Select(s => ElidePossiblyMultilineString(config, s, indent + 4, valueSize - 3) + (s == "..." ? "" : ",")))
                + "\n" + new string(' ', indent)
                + "}";
        }

        return "{ " + string.Join(", ", contents) + " }";
    }

    static IEnumerable<string> PrintListContents(ExpressionToCodeConfiguration config, IEnumerable list)
    {
        var count = 0;
        foreach (var item in list) {
            count++;
            if (count > config.PrintedListLengthLimit) {
                yield return "...";
                yield break;
            } else {
                yield return ComplexObjectToPseudoCode(config, item, 0);
            }
        }
    }

    static readonly ConcurrentDictionary<(Type, Type), IInitializerStringifier> initializerStringifiers = new();

    public static string PrintInitializerContents(ExpressionToCodeConfiguration config, IEnumerable list, Type keyType, Type valueType, int indent, int valueSize)
    {
        if (!initializerStringifiers.TryGetValue((keyType, valueType), out var stringify)) {
            stringify = (IInitializerStringifier)(Activator.CreateInstance(typeof(InitializerStringifier<,>).MakeGenericType(keyType, valueType)) ?? throw new("InitializerStringifier is constructable"));
            initializerStringifiers[(keyType, valueType)] = stringify;
        }

        return FormatLiteralInitializerContents(config, stringify.PrintInitializerContents(config, list).ToArray(), indent, valueSize);
    }

    interface IInitializerStringifier
    {
        IEnumerable<string> PrintInitializerContents(ExpressionToCodeConfiguration config, IEnumerable list);
    }

    sealed class InitializerStringifier<TKey, TValue> : IInitializerStringifier
    {
        static IEnumerable<string> PrintInitializerContents(ExpressionToCodeConfiguration config, IEnumerable<KeyValuePair<TKey, TValue>> list)
        {
            var count = 0;
            foreach (var item in list) {
                count++;
                if (count > config.PrintedListLengthLimit) {
                    yield return "...";
                    yield break;
                } else {
                    yield return $"[{ComplexObjectToPseudoCode(config, item.Key, 0)}] = {ComplexObjectToPseudoCode(config, item.Value, 0)}";
                }
            }
        }

        public IEnumerable<string> PrintInitializerContents(ExpressionToCodeConfiguration config, IEnumerable list)
            => PrintInitializerContents(config, (IEnumerable<KeyValuePair<TKey, TValue>>)list);
    }

    public static string? ExpressionValueAsCode(ExpressionToCodeConfiguration config, Expression expression, int indent)
    {
        try {
            Delegate lambda;
            try {
                lambda = config.ExpressionCompiler.Compile(Expression.Lambda(expression));
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
            return "throws " + tie.InnerException?.GetType().FullName;
        }
    }

    internal static string? PlainObjectToCode(ExpressionToCodeConfiguration config, object? val, Type? type)
        => val switch {
            null when type == null || type == typeof(object) => "null",
            null => "default(" + type.ToCSharpFriendlyTypeName(config, false) + ")",
            string str => UseVerbatimSyntax(config, str) ? "@\"" + str.Replace("\"", "\"\"") + "\"" : "\"" + EscapeStringChars(str) + "\"",
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
            Enum enumVal => EnumValueToCode(config, val, enumVal),
            Type typeVal => "typeof(" + typeVal.ToCSharpFriendlyTypeName(config, false) + ")",
            MethodInfo methodInfoVal =>
                methodInfoVal.DeclaringType switch {
                    { } declaringType when declaringType.GuessTypeClass() is not (ReflectionHelpers.TypeClass.TopLevelProgramClosureType or ReflectionHelpers.TypeClass.ClosureType)
                        => declaringType.ToCSharpFriendlyTypeName(config, false) + "." + methodInfoVal.Name,
                    _ when Regex.Match(methodInfoVal.Name, @"^.+>g__(\w+)\|[\d_]*$", RegexOptions.Multiline) is { Success: true, } match =>
                        match.Groups[1].Value,
                    _ => methodInfoVal.Name,
                },
            _ when val is ValueType && (Activator.CreateInstance(val.GetType()) ?? throw new("value types cannot be null: " + val.GetType())).Equals(val) => "default(" + val.GetType().ToCSharpFriendlyTypeName(config, false) + ")",
            _ => null,
        };

    static string EnumValueToCode(ExpressionToCodeConfiguration config, object val, Enum enumVal)
    {
        if (Enum.IsDefined(enumVal.GetType(), enumVal)) {
            return enumVal.GetType().ToCSharpFriendlyTypeName(config, false) + "." + enumVal;
        } else {
            var enumAsLong = ((IConvertible)enumVal).ToInt64(null);
            var toString = enumVal.ToString();
            if (toString == enumAsLong.ToString()) {
                return "((" + enumVal.GetType().ToCSharpFriendlyTypeName(config, false) + ")" + enumAsLong + ")";
            } else {
                var components = toString.Split(new[] { ", ", }, StringSplitOptions.RemoveEmptyEntries);
                return components.Length == 0
                    ? "default(" + enumVal.GetType().ToCSharpFriendlyTypeName(config, false) + ")"
                    : components.Length == 1
                        ? enumVal.GetType().ToCSharpFriendlyTypeName(config, false) + "." + components[0]
                        : "(" + string.Join(" | ", components.Select(s => val.GetType().ToCSharpFriendlyTypeName(config, false) + "." + s)) + ")";
            }
        }
    }

    internal static bool UseVerbatimSyntax(ExpressionToCodeConfiguration config, string str)
    {
        if (!config.AllowVerbatimStringLiterals) {
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
