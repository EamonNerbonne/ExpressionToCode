using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ExpressionToCodeLib.Internal
{
    static class NullabilityHelpers
    {
        public static bool IsNullableValueType(this Type type) => type.GetTypeInfo().IsValueType && type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        public static Type EnusureNullability(this Type type) => !type.GetTypeInfo().IsValueType || type.IsNullableValueType() ? type : typeof(Nullable<>).MakeGenericType(type);

        public static Type AvoidNullability(this Type type) => !type.GetTypeInfo().IsValueType || !type.GetTypeInfo().IsGenericType || type.GetGenericTypeDefinition() != typeof(Nullable<>)
            ? type
            : type.GetTypeInfo().GetGenericArguments()[0];
    }
}
