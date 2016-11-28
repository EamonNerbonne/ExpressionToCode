using System;
using System.Collections.Generic;
using System.Linq;
using ExpressionToCodeLib.Internal;

namespace ExpressionToCodeLib
{
    public static class ObjectToCode
    {
        public static string ComplexObjectToPseudoCode(object val) => ObjectToCodeImpl.ComplexObjectToPseudoCode(ExpressionToCodeConfiguration.CurrentConfiguration, val, 0);
        public static string ComplexObjectToPseudoCode(ExpressionToCodeConfiguration config, object val) => ObjectToCodeImpl.ComplexObjectToPseudoCode(config, val, 0);

        public static string PlainObjectToCode(object val) => ExpressionToCodeConfiguration.CurrentConfiguration.Value.ObjectStringifier.PlainObjectToCode(val, val?.GetType());
        public static string PlainObjectToCode(object val, Type type) => ExpressionToCodeConfiguration.CurrentConfiguration.Value.ObjectStringifier.PlainObjectToCode(val, type);
        public static string ToCSharpFriendlyTypeName(this Type type) => new CSharpFriendlyTypeName { IncludeGenericTypeArgumentNames = true }.GetTypeName(type);
    }

}
