namespace ExpressionToCodeLib;

/// <summary>
///     If you wish to override some formatting aspects of these methods, set
///     ExpressionToCodeConfiguration.GlobalCodeGetConfiguration.
/// </summary>
public static class ObjectToCode
{
    public static string ComplexObjectToPseudoCode(object? val)
        => ObjectToCodeImpl.ComplexObjectToPseudoCode(ExpressionToCodeConfiguration.GlobalCodeGenConfiguration, val, 0);

    public static string ComplexObjectToPseudoCode(this ExpressionToCodeConfiguration config, object? val)
        => ObjectToCodeImpl.ComplexObjectToPseudoCode(config, val, 0);

    public static string? PlainObjectToCode(object? val)
        => ObjectToCodeImpl.PlainObjectToCode(ExpressionToCodeConfiguration.GlobalCodeGenConfiguration, val, val?.GetType());

    public static string? PlainObjectToCode(object? val, Type? type)
        => ObjectToCodeImpl.PlainObjectToCode(ExpressionToCodeConfiguration.GlobalCodeGenConfiguration, val, type);

    public static string ToCSharpFriendlyTypeName(this Type type)
        => new CSharpFriendlyTypeName { IncludeGenericTypeArgumentNames = true, }.GetTypeName(type);

    public static string ToCSharpFriendlyTypeName(this Type type, ExpressionToCodeConfiguration config)
        => type.ToCSharpFriendlyTypeName(config.UseFullyQualifiedTypeNames, true);

    public static string ToCSharpFriendlyTypeName(this Type type, bool useFullyQualifiedTypeNames, bool includeGenericTypeArgumentNames)
        => new CSharpFriendlyTypeName { IncludeGenericTypeArgumentNames = includeGenericTypeArgumentNames, UseFullyQualifiedTypeNames = useFullyQualifiedTypeNames, }.GetTypeName(type);
}
