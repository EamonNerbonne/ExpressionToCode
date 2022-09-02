namespace ExpressionToCodeTest;

public sealed class ApiStabilityTest
{
    [Fact]
    public void PublicApi()
    {
        var publicTypes = typeof(ExpressionToCode).GetTypeInfo().Assembly.GetTypes()
            .Where(IsPublic)
            .Where(type => !type.Namespace!.Contains("Unstable"))
            .OrderByDescending(type => type.GetTypeInfo().IsEnum)
            .ThenByDescending(type => type.GetTypeInfo().IsInterface)
            .ThenBy(type => type.FullName);

        ApprovalTest.Verify(PrettyPrintTypes(publicTypes));
    }

    [Fact]
    public void UnstableApi()
    {
        var unstableTypes = typeof(ExpressionToCode).GetTypeInfo().Assembly.GetTypes()
            .Where(IsPublic)
            .Where(type => type.Namespace!.Contains("Unstable"))
            .OrderByDescending(type => type.GetTypeInfo().IsEnum)
            .ThenByDescending(type => type.GetTypeInfo().IsInterface)
            .ThenBy(type => type.FullName);

        ApprovalTest.Verify(PrettyPrintTypes(unstableTypes));
    }

    static string PrettyPrintTypes(IEnumerable<Type> types)
        => string.Join("", types.Select(PrettyPrintTypeDescription));

    static string PrettyPrintTypeDescription(Type o)
        => PrettyPrintTypeHeader(o) + "\n" + PrettyPrintTypeContents(o);

    static string PrettyPrintTypeContents(Type type)
    {
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .OrderBy(mi => mi.MetadataToken)
                .Where(mi => mi.DeclaringType!.GetTypeInfo().Assembly != typeof(object).GetTypeInfo().Assembly) //exclude noise
            ;

        var methodBlock = string.Join("", methods.Select(mi => PrettyPrintMethod(mi) + "\n"));

        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(mi => mi.DeclaringType!.GetTypeInfo().Assembly != typeof(object).GetTypeInfo().Assembly) //exclude noise
            ;

        var fieldBlock = string.Join("", fields.Select(fi => PrettyPrintField(fi) + "\n"));

        return fieldBlock + methodBlock + "\n";
    }

    static string PrettyPrintTypeHeader(Type type)
    {
        var prefix = TypePrefix(type);
        var baseType = type.GetTypeInfo().BaseType;
        var inheritanceTypes = baseType == typeof(object) || baseType == null
            ? type.GetInterfaces()
            : new[] { baseType }.Concat(type.GetInterfaces().Except(baseType.GetInterfaces()));
        var suffix = !inheritanceTypes.Any() || type.GetTypeInfo().IsEnum ? "" : " : " + string.Join(", ", inheritanceTypes.Select(ObjectToCode.ToCSharpFriendlyTypeName));
        var name = type.ToCSharpFriendlyTypeName();
        return prefix + " " + name + suffix;
    }

    static string TypePrefix(Type type)
    {
        if (type.GetTypeInfo().IsEnum) {
            return "enum";
        } else if (type.GetTypeInfo().IsValueType) {
            return "struct";
        } else if (type.GetTypeInfo().IsInterface) {
            return "interface";
        } else {
            return "class";
        }
    }

    static string PrettyPrintMethod(MethodInfo mi)
    {
        var fakeTarget = mi.IsStatic ? "TYPE" : "inst";

        return "    " + mi.ReturnType.ToCSharpFriendlyTypeName() + " " + fakeTarget +
            "." + mi.Name
            + PrettyPrintGenericArguments(mi)
            + PrettyPrintParameterList(mi);
    }

    static object PrettyPrintField(FieldInfo fi)
        => "    "
            + (fi.IsLiteral ? "const " : (fi.IsStatic ? "static " : "") + (fi.IsInitOnly ? "readonly " : ""))
            + fi.FieldType.ToCSharpFriendlyTypeName()
            + " " + fi.Name
            + (fi.IsLiteral ? " = " + ObjectToCode.ComplexObjectToPseudoCode(fi.GetRawConstantValue()) : "");

    static string PrettyPrintParameterList(MethodInfo mi)
        => "(" + string.Join(
            ", ",
            mi.GetParameters()
                .Select(
                    pi =>
                        pi.ParameterType.ToCSharpFriendlyTypeName() + " " + pi.Name)) + ")";

    static string PrettyPrintGenericArguments(MethodInfo mi)
    {
        if (!mi.IsGenericMethodDefinition) {
            return "";
        }

        return "<"
            + string.Join(", ", mi.GetGenericArguments().Select(ObjectToCode.ToCSharpFriendlyTypeName))
            + ">";
    }

    static bool IsPublic(Type type)
        // ReSharper disable once ConstantNullCoalescingCondition
        => type.GetTypeInfo().IsPublic || type.GetTypeInfo().IsNestedPublic && IsPublic(type.DeclaringType ?? throw new InvalidOperationException("A nested public type has no declaring type" + type));
}
