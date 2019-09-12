using System;

namespace ExpressionToCodeLib
{
    public interface IObjectStringifier
    {
        string? PlainObjectToCode(object? val, Type? type);
        string TypeNameToCode(Type type);
    }

    public static class ObjectStringifierExtensions
    {
        public static string PlainObjectToCode(this IObjectStringifier it, object? val)
            => it.PlainObjectToCode(val, val?.GetType());
    }
}
