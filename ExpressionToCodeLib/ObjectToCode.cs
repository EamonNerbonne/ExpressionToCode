using System;
using System.Collections.Generic;
using System.Linq;
using ExpressionToCodeLib.Internal;

namespace ExpressionToCodeLib
{
    public static class ObjectToCode
    {
        public static string ComplexObjectToPseudoCode(object val, int indent = 0)
            => ObjectToCodeImpl.ComplexObjectToPseudoCode(ExpressionToCodeConfiguration.CurrentConfiguration, val, indent);

        public static string PlainObjectToCode(object val) => ObjectToCodeImpl.PlainObjectToCode(ExpressionToCodeConfiguration.CurrentConfiguration, val, val?.GetType());
        public static string PlainObjectToCode(object val, Type type) => ObjectToCodeImpl.PlainObjectToCode(ExpressionToCodeConfiguration.CurrentConfiguration, val, type);
        public static string ToCSharpFriendlyTypeName(this Type type) => new CSharpFriendlyTypeName { IncludeGenericTypeArgumentNames = true }.GetTypeName(type);
    }
}
