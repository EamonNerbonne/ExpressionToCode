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
        public static string ToCode<T, T1, T2, T3>(Expression<Func<T, T1, T2, T3>> e) => ToCode((Expression)e);
        public static string ToCode<T, T1, T2>(Expression<Func<T, T1, T2>> e) => ToCode((Expression)e);
        public static string ToCode<T, T1>(Expression<Func<T, T1>> e) => ToCode((Expression)e);
        public static string ToCode<T>(Expression<Func<T>> e) => ToCode((Expression)e);
        public static string AnnotatedToCode<T, T1, T2, T3>(Expression<Func<T, T1, T2, T3>> e) => AnnotatedToCode((Expression)e);
        public static string AnnotatedToCode<T, T1, T2>(Expression<Func<T, T1, T2>> e) => AnnotatedToCode((Expression)e);
        public static string AnnotatedToCode<T, T1>(Expression<Func<T, T1>> e) => AnnotatedToCode((Expression)e);
        public static string AnnotatedToCode<T>(Expression<Func<T>> e) => AnnotatedToCode((Expression)e);
        internal static bool ShouldIgnoreSpaceAfter(char c) => c == ' ' || c == '(';
        public static string ToCode(Expression e) => ExpressionStringify.Default.ToCode(e);
        public static string AnnotatedToCode(Expression expr) => PAssertConfiguration.CurrentConfiguration.CodeAnnotator.AnnotateExpressionTree(expr, null, false);
    }
}
