using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ExpressionToCodeLib.Internal
{
#if dotnet_low
    class TypeInfo
    {
        readonly Type type;
        public TypeInfo(Type type) => this.type = type;
        public bool IsGenericType => type.IsGenericType;
        public bool IsGenericTypeDefinition => type.IsGenericTypeDefinition;
        public bool IsInterface => type.IsInterface;
        public bool IsEnum => type.IsEnum;
        public bool IsValueType => type.IsValueType;
        public bool IsArray => type.IsArray;
        public bool IsPrimitive => type.IsPrimitive;
        public Type[] GetGenericArguments() => type.GetGenericArguments();
        public bool IsAssignableFrom(Type rightCType) => type.IsAssignableFrom(rightCType);
        public InterfaceMapping GetRuntimeInterfaceMap(Type genEquatable) => type.GetInterfaceMap(genEquatable);
        public Type[] GetInterfaces() => type.GetInterfaces();
        public Type GetEnumUnderlyingType() => type.GetEnumUnderlyingType();
        public MethodInfo GetMethod(string name, Type[] types) => type.GetMethod(name, types);
        public MethodInfo[] GetMethods(BindingFlags bindingFlags) => type.GetMethods(bindingFlags);
        public MethodInfo GetMethod(string name) => type.GetMethod(name);
        public ConstructorInfo[] GetConstructors() => type.GetConstructors();
        public PropertyInfo[] GetProperties() => type.GetProperties();
        public PropertyInfo GetProperty(string pName, BindingFlags bindingFlags) => type.GetProperty(pName, bindingFlags);
        public object[] GetCustomAttributes(Type attributeType, bool inherit) => type.GetCustomAttributes(attributeType, inherit);
    }
#endif

    static class DotNet40Compat
    {
#if dotnet_low
        public static TypeInfo GetTypeInfo(this Type type) => new TypeInfo(type);
        public static MethodInfo GetMethodInfo(this Delegate method) => method.Method;
        public static MethodInfo Getter(this PropertyInfo property) => property.GetGetMethod();
#else
        public static MethodInfo Getter(this PropertyInfo property) => property.GetMethod;
#endif
    }
}
