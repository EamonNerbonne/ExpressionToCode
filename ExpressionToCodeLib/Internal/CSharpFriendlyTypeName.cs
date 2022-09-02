namespace ExpressionToCodeLib.Internal;

struct CSharpFriendlyTypeName
{
    public bool UseFullName;
    public bool IncludeGenericTypeArgumentNames;

    public string GetTypeName(Type type)
        => AliasNameOrNull(type) ?? NullableTypeNameOrNull(type.GetTypeInfo()) ?? ArrayTypeNameOrNull(type) ?? ValueTupleTypeNameOrNull(type) ?? GetUnaliasedTypeName(type);

    string? ValueTupleTypeNameOrNull(Type type)
    {
        if (!IsValueTupleType(type.GetTypeInfo())) {
            return null;
        }

        var output = new StringBuilder();
        output.Append("(");
        var genericArguments = type.GetTypeInfo().GetGenericArguments();
        var nextIdx = 0;
        while (nextIdx < genericArguments.Length) {
            var typePar = genericArguments[nextIdx];
            if (nextIdx + 1 == genericArguments.Length) {
                if (nextIdx == 7 && IsValueTupleType(typePar.GetTypeInfo())) {
                    genericArguments = typePar.GetTypeInfo().GetGenericArguments();
                    nextIdx = 0;
                } else {
                    output.Append(GetTypeName(typePar));
                    break;
                }
            } else {
                output.Append(GetTypeName(typePar));
                output.Append(", ");
                nextIdx++;
            }
        }

        output.Append(")");
        return output.ToString();
    }

    public static bool IsValueTupleType(TypeInfo typeInfo)
        => typeInfo.IsGenericType && !typeInfo.IsGenericTypeDefinition && typeInfo.Namespace == "System" && typeInfo.Name.StartsWith("ValueTuple`", StringComparison.Ordinal);

    string GetUnaliasedTypeName(Type type)
    {
        var typeNameWithoutNamespace =
            GenericTypeName(type)
            ?? NormalName(type);
        return UseFullName ? type.Namespace + "." + typeNameWithoutNamespace : typeNameWithoutNamespace;
    }

    static string? AliasNameOrNull(Type type)
        => type switch {
            _ when type == typeof(bool) => "bool",
            _ when type == typeof(byte) => "byte",
            _ when type == typeof(sbyte) => "sbyte",
            _ when type == typeof(char) => "char",
            _ when type == typeof(decimal) => "decimal",
            _ when type == typeof(double) => "double",
            _ when type == typeof(float) => "float",
            _ when type == typeof(int) => "int",
            _ when type == typeof(uint) => "uint",
            _ when type == typeof(long) => "long",
            _ when type == typeof(ulong) => "ulong",
            _ when type == typeof(object) => "object",
            _ when type == typeof(short) => "short",
            _ when type == typeof(ushort) => "ushort",
            _ when type == typeof(string) => "string",
            _ when type == typeof(void) => "void",
            { IsGenericParameter: true } => type.Name,
            _ => null,
        };

    string? NullableTypeNameOrNull(TypeInfo typeInfo)
        => typeInfo.IsGenericType && !typeInfo.IsGenericTypeDefinition && typeInfo.GetGenericTypeDefinition() == typeof(Nullable<>) ? GetTypeName(typeInfo.GetGenericArguments().Single()) + "?" : null;

    string NormalName(Type type)
    {
        if (type.DeclaringType != null) {
            var settingsWithoutUseFullname = this;
            settingsWithoutUseFullname.UseFullName = false;

            return settingsWithoutUseFullname.GetTypeName(type.DeclaringType) + "." + type.Name;
        } else {
            return type.Name;
        }
    }

    string? GenericTypeName(Type? type)
    {
        if (type ==null || !type.GetTypeInfo().IsGenericType) {
            return null;
        }

        var renderAsGenericTypeDefinition = !IncludeGenericTypeArgumentNames && type.GetTypeInfo().IsGenericTypeDefinition;

        var typeArgs = type.GetTypeInfo().GetGenericArguments();
        var typeArgIdx = typeArgs.Length;
        var revNestedTypeNames = new List<string>();

        while (type != null) {
            var name = type.Name;
            var backtickIdx = name.IndexOf('`');
            if (backtickIdx == -1) {
                revNestedTypeNames.Add(name);
            } else {
                var afterArgCountIdx = name.IndexOf('[', backtickIdx + 1);
                if (afterArgCountIdx == -1) {
                    afterArgCountIdx = name.Length;
                }

                var thisTypeArgCount = int.Parse(name.Substring(backtickIdx + 1, afterArgCountIdx - backtickIdx - 1));
                if (renderAsGenericTypeDefinition) {
                    typeArgIdx -= thisTypeArgCount;
                    revNestedTypeNames.Add(name.Substring(0, backtickIdx) + "<" + new string(',', thisTypeArgCount - 1) + ">");
                } else {
                    var argNames = new List<string>();
                    for (var i = typeArgIdx - thisTypeArgCount; i < typeArgIdx; i++) {
                        argNames.Add(GetTypeName(typeArgs[i]));
                    }

                    typeArgIdx -= thisTypeArgCount;
                    revNestedTypeNames.Add(name.Substring(0, backtickIdx) + "<" + string.Join(", ", argNames) + ">");
                }
            }

            type = type.DeclaringType;
        }

        revNestedTypeNames.Reverse();
        return string.Join(".", revNestedTypeNames);
    }

    string? ArrayTypeNameOrNull(Type type)
    {
        if (!type.IsArray) {
            return null;
        }

        var arraySuffix = default(string?);
        do {
            var rankCommas = new string(',', type.GetArrayRank() - 1);
            type = type.GetElementType() ?? throw new("Arrays must have an element type");
            arraySuffix = arraySuffix + "[" + rankCommas + "]";
            // ReSharper disable once PossibleNullReferenceException
        } while (type.IsArray);

        var basename = GetTypeName(type);
        return basename + arraySuffix;
    }
}
