using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace ExpressionToCodeLib
{
    internal class SubExpressionPerLineCodeAnnotator : ICodeAnnotator
    {
        public string AnnotateExpressionTree(Expression expr, string msg, bool ignoreOuterMostValue)
        {
            return (msg == null ? "" : msg + "\n") + ExpressionWithSubExpressions.Create(expr, ignoreOuterMostValue).ComposeToSingleString();
        }

        struct ExpressionWithSubExpressions
        {
            string ExpressionString;
            SubExpressionValue[] SubExpressions;

            public struct SubExpressionValue
            {
                public string SubExpression, ValueAsString;
            }

            public static ExpressionWithSubExpressions Create(Expression e, bool ignoreOutermostValue)
            {
                var sb = new StringBuilder();
                bool ignoreInitialSpace = true;
                var node = new ExpressionToCodeImpl().ExpressionDispatch(e);
                AppendNodeToStringBuilder(sb, node, ref ignoreInitialSpace);
                var fullExprText = sb.ToString();
                var subExpressionValues = new List<SubExpressionValue>();
                FindSubExpressionValues(node, node, subExpressionValues, ignoreOutermostValue);
                return new ExpressionWithSubExpressions { ExpressionString = fullExprText, SubExpressions = subExpressionValues.ToArray() };
            }

            static void AppendNodeToStringBuilder(StringBuilder sb, StringifiedExpression node, ref bool ignoreInitialSpace)
            {
                if (node.Text != null) {
                    var trimmedText = ignoreInitialSpace ? node.Text.TrimStart() : node.Text;
                    sb.Append(trimmedText);
                    ignoreInitialSpace = node.Text != "" && ExpressionToCode.ShouldIgnoreSpaceAfter(node.Text[node.Text.Length - 1]);
                } else {
                    foreach (var kid in node.Children) {
                        AppendNodeToStringBuilder(sb, kid, ref ignoreInitialSpace);
                    }
                }
            }

            static void FindSubExpressionValues(
                StringifiedExpression node,
                StringifiedExpression subExprNode,
                List<SubExpressionValue> subExpressionValues,
                bool ignoreOutermostValue)
            {
                if (!ignoreOutermostValue && node.OptionalValue != null) {
                    var sb = new StringBuilder();
                    var ignoreInitialSpace = true;
                    AppendNodeWithLimitedDepth(sb, subExprNode, ref ignoreInitialSpace, 1);
                    var subExprString = sb.ToString();
                    string valueString = ObjectToCode.ExpressionValueAsCode(node.OptionalValue);
                    subExpressionValues.Add(new SubExpressionValue { SubExpression = subExprString, ValueAsString = valueString });
                }
                foreach (var kid in node.Children) {
                    if (kid.IsConceptualChild) {
                        FindSubExpressionValues(kid, kid, subExpressionValues, false);
                    } else {
                        FindSubExpressionValues(kid, subExprNode, subExpressionValues, ignoreOutermostValue);
                    }
                }
            }

            static void AppendNodeWithLimitedDepth(StringBuilder sb, StringifiedExpression node, ref bool ignoreInitialSpace, int unfoldToDepth)
            {
                if (node.Text != null) {
                    var trimmedText = ignoreInitialSpace ? node.Text.TrimStart() : node.Text;
                    sb.Append(trimmedText);
                    ignoreInitialSpace = node.Text != "" && ExpressionToCode.ShouldIgnoreSpaceAfter(node.Text[node.Text.Length - 1]);
                } else {
                    foreach (var kid in node.Children) {
                        if (kid.IsConceptualChild && unfoldToDepth == 0) {
                            sb.Append("...");
                        } else {
                            AppendNodeWithLimitedDepth(sb, kid, ref ignoreInitialSpace, unfoldToDepth - (kid.IsConceptualChild ? 1 : 0));
                        }
                    }
                }
            }

            public string ComposeToSingleString()
            {
                return ExpressionString + "\n" + String.Join("", SubExpressions.Select(sub => sub.SubExpression + ": " + sub.ValueAsString + "\n"));
            }
        }
    }
}
