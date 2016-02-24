using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ExpressionToCodeLib.Internal;
using ExpressionToCodeLib.Unstable_v2_Api;

namespace ExpressionToCodeLib
{
    internal static class ObjectToCodeImpl
    {
        internal static string ComplexObjectToPseudoCode(ExpressionToCodeConfiguration config, object val, int indent)
        {
            string retval = ObjectToCode.PlainObjectToCode(val);
            if (retval != null) {
                return retval;
            } else if (val is Array) {
                return "new[] " + FormatEnumerable(config, (IEnumerable)val);
            } else if (val is IEnumerable) {
                return FormatEnumerable(config, (IEnumerable)val);
            } else if (val is Expression) {
                return new ExpressionStringify(config).ToCode((Expression)val);
            } else if (val.GetType().GuessTypeClass() == ReflectionHelpers.TypeClass.AnonymousType) {
                var type = val.GetType();
                return "\n" + new string(' ', indent * 2) +
                    "new {" +
                    string.Join(
                        "",
                        type.GetProperties()
                            .Select(
                                pi =>
                                    "\n" + new string(' ', indent * 2 + 2) + pi.Name + " = "
                                        + ComplexObjectToPseudoCode(config, pi.GetValue(val, null), indent + 2) + ",")
                        )
                    + "\n" + new string(' ', indent * 2) + "}";
            } else {
                return val.ToString();
            }
        }

        static string FormatEnumerable(ExpressionToCodeConfiguration config, IEnumerable list) => "{" + string.Join(", ", ExtractFirst10(config, list).ToArray()) + "}";

        static IEnumerable<string> ExtractFirst10(ExpressionToCodeConfiguration config, IEnumerable list)
        {
            int count = 0;
            foreach (var item in list) {
                count++;
                if (count > 10) {
                    yield return "...";
                    yield break;
                } else {
                    yield return ComplexObjectToPseudoCode(config, item, 0);
                }
            }
        }

        public static string PlainObjectToCode(ExpressionToCodeConfiguration config, object val, Type type) => config.Value.ObjectStringifier.PlainObjectToCode(val, type);
    }

    public static class ObjectToCode
    {
        public static string ComplexObjectToPseudoCode(object val, int indent = 0)
            => ObjectToCodeImpl.ComplexObjectToPseudoCode(ExpressionToCodeConfiguration.CurrentConfiguration, val, indent);

        public static string PlainObjectToCode(object val) => ObjectToCodeImpl.PlainObjectToCode(ExpressionToCodeConfiguration.CurrentConfiguration, val, val?.GetType());
        public static string PlainObjectToCode(object val, Type type) => ObjectToCodeImpl.PlainObjectToCode(ExpressionToCodeConfiguration.CurrentConfiguration, val, type);
        public static string GetCSharpFriendlyTypeName(Type type) => new CSharpFriendlyTypeName { IncludeGenericTypeArgumentNames = true }.GetTypeName(type);

        internal static string ExpressionValueAsCode(Expression expression)
        {
            try {
                Delegate lambda;
                try {
                    lambda = Expression.Lambda(expression).Compile();
                } catch (InvalidOperationException) {
                    return null;
                }

                var val = lambda.DynamicInvoke();
                try {
                    return ObjectToCode.ComplexObjectToPseudoCode(val);
                } catch (Exception e) {
                    return "stringification throws " + e.GetType().FullName;
                }
            } catch (TargetInvocationException tie) {
                return "throws " + tie.InnerException.GetType().FullName;
            }
        }
    }
}
