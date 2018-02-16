using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ExpressionToCodeLib.Internal
{
    struct CSharpFriendlyTypeName
    {
        public bool UseFullName;
        public bool IncludeGenericTypeArgumentNames;

        public string GetTypeName(Type type)
            => AliasName(type) ?? GetUnaliasedTypeName(type);

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
            } else if (type.GetTypeInfo().IsGenericType && type != typeof(Nullable<>) && type.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                return GetTypeName(type.GetTypeInfo().GetGenericArguments().Single()) + "?";
            } else if (type.IsGenericParameter) {
                return type.Name;
            } else {
                return ArrayTypeName(type);
            }
        }

        string NormalName(Type type)
        {
            if (type.DeclaringType != null) {
                var settingsWithoutUseFullname = this;
                settingsWithoutUseFullname.UseFullName = false;

                return settingsWithoutUseFullname.GetTypeName(type.DeclaringType) + "." + type.Name;
            } else {
                return type.Name;
            }
        }

        string GenericTypeName(Type type)
        {
            if (!type.GetTypeInfo().IsGenericType) {
                return null;
            }

            var renderAsGenericTypeDefinition = !IncludeGenericTypeArgumentNames && type.GetTypeInfo().IsGenericTypeDefinition;

            var typeArgs = type.GetTypeInfo().GetGenericArguments();
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
                        for (var i = typeArgIdx - thisTypeArgCount; i < typeArgIdx; i++) {
                            argNames.Add(GetTypeName(typeArgs[i]));
                        }
                        typeArgIdx -= thisTypeArgCount;
                        revNestedTypeNames.Add(name.Substring(0, backtickIdx) + "<" + string.Join(", ", argNames) + ">");
                    }
                }
                type = type.DeclaringType;
            }
            revNestedTypeNames.Reverse();
            return string.Join(".", revNestedTypeNames);
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
                // ReSharper disable once PossibleNullReferenceException
            } while (type.IsArray);
            var basename = GetTypeName(type);
            return basename + arraySuffix;
        }
    }
}
