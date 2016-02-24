using System;

namespace ExpressionToCodeLib.Unstable_v2_Api
{
    public static class ObjectToCodeExt
    {
        public static string PlainObjectToCode(this IObjectToCode it, object val) => it.PlainObjectToCode(val, val == null ? null : val.GetType());
    }
}
