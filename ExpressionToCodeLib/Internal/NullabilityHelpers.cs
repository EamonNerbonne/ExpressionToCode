namespace ExpressionToCodeLib.Internal;

static class NullabilityHelpers
{
    static bool IsNullableValueType(this Type type)
        => type.GetTypeInfo().IsValueType && type.GetTypeInfo().IsGenericType
            && type.GetGenericTypeDefinition() == typeof(Nullable<>);

    public static Type EnusureNullability(this Type type)
        => !type.GetTypeInfo().IsValueType || type.IsNullableValueType()
            ? type
            : typeof(Nullable<>).MakeGenericType(type);

    public static Type AvoidNullability(this Type type)
        => !type.GetTypeInfo().IsValueType || !type.GetTypeInfo().IsGenericType
            || type.GetGenericTypeDefinition() != typeof(Nullable<>)
                ? type
                : type.GetTypeInfo().GetGenericArguments()[0];
}
