using System;
using System.Collections.Generic;
using System.Linq;
using ExpressionToCodeLib.Internal;

namespace ExpressionToCodeLib
{
    /// <summary>
    ///     If you wish to override some formatting aspects of these methods, set
    ///     ExpressionToCodeConfiguration.GlobalCodeGetConfiguration.
    /// </summary>
    public static class ObjectToCode
    {
        public static string ComplexObjectToPseudoCode(object val)
            => ObjectToCodeImpl.ComplexObjectToPseudoCode(
                ExpressionToCodeConfiguration.GlobalCodeGenConfiguration,
                val,
                0);

        public static string ComplexObjectToPseudoCode(this ExpressionToCodeConfiguration config, object val)
            => ObjectToCodeImpl.ComplexObjectToPseudoCode(config, val, 0);

        public static string PlainObjectToCode(object? val)
            => ExpressionToCodeConfiguration.GlobalCodeGenConfiguration.Value.ObjectStringifier.PlainObjectToCode(
                val,
                val?.GetType());

        public static string PlainObjectToCode(object? val, Type type)
            => ExpressionToCodeConfiguration.GlobalCodeGenConfiguration.Value.ObjectStringifier.PlainObjectToCode(
                val,
                type);

        public static string ToCSharpFriendlyTypeName(this Type type)
            => new CSharpFriendlyTypeName { IncludeGenericTypeArgumentNames = true }.GetTypeName(type);
    }
}
