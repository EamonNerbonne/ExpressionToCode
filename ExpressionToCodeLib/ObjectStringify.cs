namespace ExpressionToCodeLib;

public static class ObjectStringify
{
    public static readonly IObjectStringifier Default = new ObjectStringifyImpl();
    public static readonly IObjectStringifier WithFullTypeNames = new ObjectStringifyImpl(true);
    public static readonly IObjectStringifier WithoutLiteralStrings = new ObjectStringifyImpl(false, false);
    public static readonly IObjectStringifier WithFullTypeNamesWithoutLiteralStrings = new ObjectStringifyImpl(true, false);
}
