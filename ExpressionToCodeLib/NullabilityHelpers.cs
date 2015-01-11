using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExpressionToCodeLib {
    static class NullabilityHelpers {
        public static bool IsNullableValueType(this Type type) { return type.IsValueType && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>); }
        public static Type EnusureNullability(this Type type) { return !type.IsValueType || type.IsNullableValueType() ? type : typeof(Nullable<>).MakeGenericType(type); }

        public static Type AvoidNullability(this Type type) {
            return !type.IsValueType || !type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Nullable<>)
                ? type
                : type.GetGenericArguments()[0];
        }
    }
}
