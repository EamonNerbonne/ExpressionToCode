namespace ExpressionToCodeLib;

public interface IObjectStringifier
{
    [Obsolete]
    string? PlainObjectToCode(object? val, Type? type);

    [Obsolete]
    string TypeNameToCode(Type type);
}

public static class ObjectStringifierExtensions
{
    public static string? PlainObjectToCode(this IObjectStringifier it, object? val)
        => it.PlainObjectToCode(val, val?.GetType());
}
