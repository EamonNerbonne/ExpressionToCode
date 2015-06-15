using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ApprovalTests;
using ExpressionToCodeLib;
using NUnit.Framework;

namespace ExpressionToCodeTest
{
    public class ApiStabilityTest
    {
        [Test, MethodImpl(MethodImplOptions.NoInlining)]
        public void PublicApi()
        {
            var publicTypes = typeof(ExpressionToCode).Assembly.GetTypes()
                .Where(IsPublic)
                .Where(type => !type.Namespace.Contains("Unstable"))
                .OrderByDescending(type => type.IsEnum)
                .ThenByDescending(type => type.IsInterface)
                .ThenBy(type => type.FullName);


            Approvals.Verify(PrettyPrintTypes(publicTypes));
        }

        [Test, MethodImpl(MethodImplOptions.NoInlining)]
        public void UnstableApi() {
            var unstableTypes = typeof(ExpressionToCode).Assembly.GetTypes()
                .Where(IsPublic)
                .Where(type => type.Namespace.Contains("Unstable"))
                .OrderByDescending(type => type.IsEnum)
                .ThenByDescending(type => type.IsInterface)
                .ThenBy(type => type.FullName);


            Approvals.Verify(PrettyPrintTypes(unstableTypes));
        }


        static string PrettyPrintTypes(IEnumerable<Type> types) { return string.Join("", types.Select(PrettyPrintTypeDescription)); }
        static string PrettyPrintTypeDescription(Type o) { return PrettyPrintTypeHeader(o) + "\n" + PrettyPrintTypeContents(o); }

        static string PrettyPrintTypeContents(Type type)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(mi => mi.DeclaringType.Assembly != typeof(object).Assembly) //exclude noise
                ;

            var methodBlock = string.Join("", methods.Select(mi => PrettyPrintMethod(mi) + "\n"));

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(mi => mi.DeclaringType.Assembly != typeof(object).Assembly) //exclude noise
                ;

            var fieldBlock = string.Join("", fields.Select(fi => PrettyPrintField(fi) + "\n"));

            return fieldBlock + methodBlock + "\n";
        }

        static string PrettyPrintTypeHeader(Type type)
        {
            var prefix = type.IsEnum ? "enum" : type.IsValueType ? "struct" : type.IsInterface ? "interface" : "class";

            var baseType = type.BaseType == typeof(object) ? null : type.BaseType;
            var allInterfaces = type.GetInterfaces();
            var interfaces = baseType == null ? allInterfaces : allInterfaces.Except(baseType.GetInterfaces());
            var inheritanceTypes = new[] { baseType }.OfType<Type>().Concat(interfaces);
            var suffix = !inheritanceTypes.Any() || type.IsEnum ? "" : " : " + string.Join(", ", inheritanceTypes.Select(ObjectToCode.GetCSharpFriendlyTypeName));

            var name = ObjectToCode.GetCSharpFriendlyTypeName(type);

            return prefix + " " + name + suffix;
        }

        static string PrettyPrintMethod(MethodInfo mi)
        {
            var fakeTarget = mi.IsStatic ? "TYPE" : "inst";

            return "    " + ObjectToCode.GetCSharpFriendlyTypeName(mi.ReturnType) + " " + fakeTarget +
                "." + mi.Name
                + PrettyPrintGenericArguments(mi)
                + PrettyPrintParameterList(mi);
        }

        static object PrettyPrintField(FieldInfo fi)
        {
            return "    "
                + (fi.IsLiteral ? "const " : (fi.IsStatic ? "static " : "") + (fi.IsInitOnly ? "readonly " : ""))
                + ObjectToCode.GetCSharpFriendlyTypeName(fi.FieldType)
                + " " + fi.Name
                + (fi.IsLiteral ? " = " + ObjectToCode.ComplexObjectToPseudoCode(fi.GetRawConstantValue()) : "")
                ;
        }

        static string PrettyPrintParameterList(MethodInfo mi)
        {
            return ("(" + string.Join(
                ", ",
                mi.GetParameters().Select(
                    pi =>
                        ObjectToCode.GetCSharpFriendlyTypeName(pi.ParameterType) + " " + pi.Name)) + ")");
        }

        static string PrettyPrintGenericArguments(MethodInfo mi)
        {
            if (!mi.IsGenericMethodDefinition) {
                return "";
            }
            return "<"
                + string.Join(", ", mi.GetGenericArguments().Select(ObjectToCode.GetCSharpFriendlyTypeName))
                + ">";
        }

        static bool IsPublic(Type type) { return type.IsPublic || type.IsNestedPublic && IsPublic(type.DeclaringType); }
    }
}
