using System;
using System.Linq.Expressions;

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

        ///<summary>
        /// Converts expression to variable/property/method C# like representation adding it's string value.
        ///</summary>
        /// <example>
        /// string toNameValueRepresentation = "Value";
        /// ToRepr(() => toNameValueRepresentation); // "toNameValueRepresentation = Value"
        /// </example>
        /// <remarks>
        /// Unlike <see cref="ExpressionToCode.ToCode"/>(which targets compilable output), this method is geared towards dumping simple objects into text, so may skip some C# issues for sake of readability.
        /// </remarks>
        public static string ToValuedCode<TResult>(this Expression<Func<TResult>> expression)
        {
            TResult retValue;
            try {
                retValue = expression.Compile().Invoke();
            } catch (Exception ex) {
                throw new InvalidOperationException("Cannon get return value of expression when it throws error", ex);
            }
            return ToCode(expression.Body) + " = " + retValue;
        }

        /// <summary>
        /// Gets property, variable or method name from lambda expression.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        /// <example>
        /// var example = "some text";
        /// var name = toName( () => example);  // "example"
        /// </example>
        public static string GetNameIn<TResult>(Expression<Func<TResult>> expression) => GetNameIn((Expression)expression);

        public static string GetNameIn(Expression<Action> expression) => GetNameIn((Expression)expression);

        public static string GetNameIn(Expression expr)
        {
            var methodCall = expr as MethodCallExpression;
            if (methodCall != null)
                return methodCall.Method.Name;

            var memberAccess = expr as MemberExpression;
            if (memberAccess != null)
                return memberAccess.Member.Name;

            if (expr.NodeType == ExpressionType.ArrayLength)
                return "Length";

            var lambda = expr as LambdaExpression;
            if (lambda != null)
                return GetNameIn(lambda.Body);

            var unary = expr as UnaryExpression;
            if (unary != null)
                return GetNameIn(unary.Operand);

            throw new ArgumentException("Unsupported or unknown or complex expression to get `name` of it", nameof(expr));
        }
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

    public interface IAnnotatedToCode
    {
        string AnnotatedToCode(Expression e, string msg, bool hideOutermostValue);
    }

    public static class AnnotatedToCodeExtensions
    {
        public static string AnnotatedToCode(this IAnnotatedToCode it, Expression e) => it.AnnotatedToCode(e, null, false);
        public static string AnnotatedToCode<T, T1, T2, T3>(this IAnnotatedToCode it, Expression<Func<T, T1, T2, T3>> e) => it.AnnotatedToCode(e, null, false);
        public static string AnnotatedToCode<T, T1, T2>(this IAnnotatedToCode it, Expression<Func<T, T1, T2>> e) => it.AnnotatedToCode(e, null, false);
        public static string AnnotatedToCode<T, T1>(this IAnnotatedToCode it, Expression<Func<T, T1>> e) => it.AnnotatedToCode(e, null, false);
        public static string AnnotatedToCode<T>(this IAnnotatedToCode it, Expression<Func<T>> e) => it.AnnotatedToCode(e, null, false);

        public static string AnnotatedToCode<T, T1, T2, T3>(this IAnnotatedToCode it, Expression<Func<T, T1, T2, T3>> e, string msg, bool hideOutermostValue)
            => it.AnnotatedToCode((Expression)e, msg, hideOutermostValue);

        public static string AnnotatedToCode<T, T1, T2>(this IAnnotatedToCode it, Expression<Func<T, T1, T2>> e, string msg, bool hideOutermostValue)
            => it.AnnotatedToCode((Expression)e, msg, hideOutermostValue);

        public static string AnnotatedToCode<T, T1>(this IAnnotatedToCode it, Expression<Func<T, T1>> e, string msg, bool hideOutermostValue)
            => it.AnnotatedToCode((Expression)e, msg, hideOutermostValue);

        public static string AnnotatedToCode<T>(this IAnnotatedToCode it, Expression<Func<T>> e, string msg, bool hideOutermostValue)
            => it.AnnotatedToCode((Expression)e, msg, hideOutermostValue);
    }
}
