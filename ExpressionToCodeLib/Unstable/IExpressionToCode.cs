using System;
using System.Linq.Expressions;

namespace ExpressionToCodeLib.Unstable
{
    public interface IExpressionToCode
    {
        string ToCode(Expression e);
    }

    public static class ExpressionToCodeExt
    {
        public static string ToCode<T, T1, T2, T3>(this IExpressionToCode it, Expression<Func<T, T1, T2, T3>> e) { return it.ToCode(e); }
        public static string ToCode<T, T1, T2>(this IExpressionToCode it, Expression<Func<T, T1, T2>> e) { return it.ToCode(e); }
        public static string ToCode<T, T1>(this IExpressionToCode it, Expression<Func<T, T1>> e) { return it.ToCode(e); }
        public static string ToCode<T>(this IExpressionToCode it, Expression<Func<T>> e) { return it.ToCode(e); }
    }
}
