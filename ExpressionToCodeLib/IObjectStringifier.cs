namespace ExpressionToCodeLib;

[Obsolete]
public interface IObjectStringifier
{
    [Obsolete]
    string? PlainObjectToCode(object? val, Type? type);

    [Obsolete]
    string TypeNameToCode(Type type);
}

[Obsolete]
public static class ObjectStringifierExtensions
{
    [Obsolete]
    public static string? PlainObjectToCode(this IObjectStringifier it, object? val)
        => it.PlainObjectToCode(val, val?.GetType());
}
