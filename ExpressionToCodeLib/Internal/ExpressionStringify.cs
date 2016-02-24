﻿using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace ExpressionToCodeLib.Internal
{
    internal sealed class ExpressionStringify : IExpressionToCode
    {
        readonly ExpressionToCodeConfiguration config;

        public ExpressionStringify(ExpressionToCodeConfiguration config)
        {
            this.config = config;
        }

        public string ToCode(Expression e) {
            return ToCodeImpl(config, e);
        }

        public static string ToCodeImpl(ExpressionToCodeConfiguration config, Expression e)
        {
            var sb = new StringBuilder();
            var ignoreInitialSpace = true;
            var stringifiedExpr = new ExpressionToCodeImpl(config).ExpressionDispatch(e);
            AppendTo(sb, ref ignoreInitialSpace, stringifiedExpr);
            return sb.ToString();
        }

        static void AppendTo(StringBuilder sb, ref bool ignoreInitialSpace, StringifiedExpression node)
        {
            if (node.Text != null) {
                sb.Append(ignoreInitialSpace ? node.Text.TrimStart() : node.Text);
                ignoreInitialSpace = node.Text.Any() && ExpressionToCode.ShouldIgnoreSpaceAfter(node.Text[node.Text.Length - 1]);
            } else {
                foreach (var kid in node.Children) {
                    AppendTo(sb, ref ignoreInitialSpace, kid);
                }
            }
        }
    }
}