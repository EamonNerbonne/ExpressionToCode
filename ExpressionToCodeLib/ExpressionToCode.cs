using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ExpressionToCodeLib.Unstable_v2_Api;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace ExpressionToCodeLib
{
    public static class ExpressionToCode
    {
        public static string ToCode<T, T1, T2, T3>(Expression<Func<T, T1, T2, T3>> e) => ExpressionToCodeConfiguration.CurrentConfiguration.GetExpressionToCode().ToCode(e);
        public static string ToCode<T, T1, T2>(Expression<Func<T, T1, T2>> e) => ExpressionToCodeConfiguration.CurrentConfiguration.GetExpressionToCode().ToCode(e);
        public static string ToCode<T, T1>(Expression<Func<T, T1>> e) => ExpressionToCodeConfiguration.CurrentConfiguration.GetExpressionToCode().ToCode(e);
        public static string ToCode<T>(Expression<Func<T>> e) => ExpressionToCodeConfiguration.CurrentConfiguration.GetExpressionToCode().ToCode(e);
        public static string ToCode(Expression e) => ExpressionToCodeConfiguration.CurrentConfiguration.GetExpressionToCode().ToCode(e);

        public static string AnnotatedToCode<T, T1, T2, T3>(Expression<Func<T, T1, T2, T3>> e) => AnnotatedToCode((Expression)e);
        public static string AnnotatedToCode<T, T1, T2>(Expression<Func<T, T1, T2>> e) => AnnotatedToCode((Expression)e);
        public static string AnnotatedToCode<T, T1>(Expression<Func<T, T1>> e) => AnnotatedToCode((Expression)e);
        public static string AnnotatedToCode<T>(Expression<Func<T>> e) => AnnotatedToCode((Expression)e);
        internal static bool ShouldIgnoreSpaceAfter(char c) => c == ' ' || c == '(';

        public static string AnnotatedToCode(Expression expr)
            =>
                ExpressionToCodeConfiguration.CurrentConfiguration.Value.CodeAnnotator.AnnotateExpressionTree(
                    ExpressionToCodeConfiguration.CurrentConfiguration,
                    expr,
                    null,
                    false);
    }

    public interface IExpressionToCode
    {
        string ToCode(Expression e);
    }

    public static class ExpressionToCodeExtensions
    {
        public static string ToCode<T, T1, T2, T3>(this IExpressionToCode it, Expression<Func<T, T1, T2, T3>> e) => it.ToCode(e);
        public static string ToCode<T, T1, T2>(this IExpressionToCode it, Expression<Func<T, T1, T2>> e) => it.ToCode(e);
        public static string ToCode<T, T1>(this IExpressionToCode it, Expression<Func<T, T1>> e) => it.ToCode(e);
        public static string ToCode<T>(this IExpressionToCode it, Expression<Func<T>> e) => it.ToCode(e);
    }
}
