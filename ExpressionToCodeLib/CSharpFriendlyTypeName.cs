using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExpressionToCodeLib
{
    struct CSharpFriendlyTypeName
    {
        public bool UseFullName;
        public bool IncludeGenericTypeArgumentNames;
        public string GetTypeName(Type type) { return AliasName(type) ?? GetUnaliasedTypeName(type); }

        string GetUnaliasedTypeName(Type type)
        {
            var typeNameWithoutNamespace =
                GenericTypeName(type)
                    ?? NormalName(type);
            return UseFullName ? type.Namespace + "." + typeNameWithoutNamespace : typeNameWithoutNamespace;
        }

        string AliasName(Type type)
        {
            if (type == typeof(bool)) {
                return "bool";
            } else if (type == typeof(byte)) {
                return "byte";
            } else if (type == typeof(sbyte)) {
                return "sbyte";
            } else if (type == typeof(char)) {
                return "char";
            } else if (type == typeof(decimal)) {
                return "decimal";
            } else if (type == typeof(double)) {
                return "double";
            } else if (type == typeof(float)) {
                return "float";
            } else if (type == typeof(int)) {
                return "int";
            } else if (type == typeof(uint)) {
                return "uint";
            } else if (type == typeof(long)) {
                return "long";
            } else if (type == typeof(ulong)) {
                return "ulong";
            } else if (type == typeof(object)) {
                return "object";
            } else if (type == typeof(short)) {
                return "short";
            } else if (type == typeof(ushort)) {
                return "ushort";
            } else if (type == typeof(string)) {
                return "string";
            } else if (type == typeof(void)) {
                return "void";
            } else if (type.IsGenericType && type != typeof(Nullable<>) && type.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                return GetTypeName(type.GetGenericArguments().Single()) + "?";
            } else {
                return ArrayTypeName(type);
            }
        }

        string NormalName(Type type)
        {
            return type.IsGenericParameter
                ? type.Name
                : type.DeclaringType != null
                    ? GetTypeName(type.DeclaringType) + "." + type.Name
                    : type.Name;
        }

        string GenericTypeName(Type type)
        {
            if (!type.IsGenericType) {
                return null;
            }

            var renderAsGenericTypeDefinition = !IncludeGenericTypeArgumentNames && type.IsGenericTypeDefinition;

            var typeArgs = type.GetGenericArguments();
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
                        for (int i = typeArgIdx - thisTypeArgCount; i < typeArgIdx; i++) {
                            argNames.Add(GetTypeName(typeArgs[i]));
                        }
                        typeArgIdx -= thisTypeArgCount;
                        revNestedTypeNames.Add(name.Substring(0, backtickIdx) + "<" + JoinTypeArgumentList(argNames) + ">");
                    }
                }
                type = type.DeclaringType;
            }
            revNestedTypeNames.Reverse();
            return string.Join(".", revNestedTypeNames);
        }

        static string JoinTypeArgumentList(List<string> argNames)
        {
            if (argNames.Count == 1) {
                return argNames[0];
            }

            var sb = new StringBuilder(argNames[0]);
            for (int i = 1; i < argNames.Count; i++) {
                var argName = argNames[i];
                if (argName == "") {
                    sb.Append(",");
                } else {
                    sb.Append(", ");
                    sb.Append(argName);
                }
            }
            return sb.ToString();
        }

        string ArrayTypeName(Type type)
        {
            if (!type.IsArray) {
                return null;
            }
            string arraySuffix = null;
            do {
                var rankCommas = new string(',', type.GetArrayRank() - 1);
                type = type.GetElementType();
                arraySuffix = arraySuffix + "[" + rankCommas + "]";
            }
            while (type.IsArray);
            string basename = GetTypeName(type);
            return basename + arraySuffix;
        }
    }
}
