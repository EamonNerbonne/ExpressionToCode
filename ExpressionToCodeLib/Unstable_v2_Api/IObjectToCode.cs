using System;

namespace ExpressionToCodeLib.Unstable_v2_Api
{
    public interface IObjectToCode
    {
        string PlainObjectToCode(object val, Type type);
        string TypeNameToCode(Type type);
    }

    public static class ObjectToCodeExt
    {
        public static string PlainObjectToCode(this IObjectToCode it, object val) { return it.PlainObjectToCode(val, val == null ? null : val.GetType()); }
    }
}
