using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using ExpressionToCodeLib.Unstable_v2_Api;

namespace ExpressionToCodeLib {
    public static class ObjectToCode {
        public static string ComplexObjectToPseudoCode(object val, int indent = 0) {
            string retval = PlainObjectToCode(val);
            if (retval != null) {
                return retval;
            } else if (val is Array) {
                return "new[] " + FormatEnumerable((IEnumerable)val);
            } else if (val is IEnumerable) {
                return FormatEnumerable((IEnumerable)val);
            } else if (val is Expression) {
                return ExpressionToCode.ToCode((Expression)val);
            } else if (val.GetType().GuessTypeClass() == ReflectionHelpers.TypeClass.AnonymousType) {
                var type = val.GetType();
                return "\n" + new string(' ', indent * 2) +
                    "new {" +
                    String.Join(
                        "",
                        type.GetProperties()
                            .Select(
                                pi =>
                                    "\n" + new string(' ', indent * 2 + 2) + pi.Name + " = "
                                        + ComplexObjectToPseudoCode(pi.GetValue(val, null), indent + 2) + ",")
                        )
                    + "\n" + new string(' ', indent * 2) + "}";
            } else {
                return val.ToString();
            }
        }

        static string FormatEnumerable(IEnumerable list) => "{" + String.Join(", ", ExtractFirst10(list).ToArray()) + "}";

        static IEnumerable<string> ExtractFirst10(IEnumerable list) {
            int count = 0;
            foreach (var item in list) {
                count++;
                if (count > 10) {
                    yield return "...";
                    yield break;
                } else {
                    yield return ComplexObjectToPseudoCode(item);
                }
            }
        }

        public static string PlainObjectToCode(object val) => PlainObjectToCode(val, val == null ? null : val.GetType());

        public static string PlainObjectToCode(object val, Type type) => ObjectStringify.Default.PlainObjectToCode(val, type);


        public static string GetCSharpFriendlyTypeName(Type type) => new CSharpFriendlyTypeName { IncludeGenericTypeArgumentNames = true }.GetTypeName(type);
    }
}
