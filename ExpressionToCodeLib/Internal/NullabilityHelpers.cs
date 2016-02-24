using System;
using System.Collections.Generic;
using System.Linq;

namespace ExpressionToCodeLib
{
    internal static class NullabilityHelpers
    {
        public static bool IsNullableValueType(this Type type) => type.IsValueType && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        public static Type EnusureNullability(this Type type) => !type.IsValueType || type.IsNullableValueType() ? type : typeof(Nullable<>).MakeGenericType(type);

        public static Type AvoidNullability(this Type type) => !type.IsValueType || !type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Nullable<>)
            ? type
            : type.GetGenericArguments()[0];
    }
}
