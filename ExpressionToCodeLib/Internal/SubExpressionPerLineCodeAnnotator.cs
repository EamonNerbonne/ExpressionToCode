using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace ExpressionToCodeLib.Internal
{
    class SubExpressionPerLineCodeAnnotator : ICodeAnnotator
    {
        public string AnnotateExpressionTree(ExpressionToCodeConfiguration config, Expression expr, string msg, bool hideOutermostValue)
        {
            return (msg == null ? "" : msg + "\n") + ExpressionWithSubExpressions.Create(config, expr, hideOutermostValue).ComposeToSingleString();
        }

        struct ExpressionWithSubExpressions
        {
            string ExpressionString;
            SubExpressionValue[] SubExpressions;

            public struct SubExpressionValue
            {
                public string SubExpression, ValueAsString;
            }

            public static ExpressionWithSubExpressions Create(ExpressionToCodeConfiguration config, Expression e, bool hideOutermostValue)
            {
                var sb = new StringBuilder();
                var ignoreInitialSpace = true;
                var node = new ExpressionToCodeImpl(config).ExpressionDispatch(e);
                AppendNodeToStringBuilder(sb, node, ref ignoreInitialSpace);
                var fullExprText = sb.ToString();
                var subExpressionValues = new List<SubExpressionValue>();
                FindSubExpressionValues(config, node, node, subExpressionValues, hideOutermostValue);
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
                ExpressionToCodeConfiguration config,
                StringifiedExpression node,
                StringifiedExpression subExprNode,
                List<SubExpressionValue> subExpressionValues,
                bool hideOutermostValue)
            {
                if (!hideOutermostValue && node.OptionalValue != null) {
                    var sb = new StringBuilder();
                    var ignoreInitialSpace = true;
                    AppendNodeWithLimitedDepth(sb, subExprNode, ref ignoreInitialSpace, 2);
                    var subExprString = sb.ToString();
                    string valueString = ObjectToCodeImpl.ExpressionValueAsCode(config, node.OptionalValue);
                    subExpressionValues.Add(new SubExpressionValue { SubExpression = subExprString, ValueAsString = valueString });
                }
                foreach (var kid in node.Children) {
                    if (kid.IsConceptualChild) {
                        FindSubExpressionValues(config, kid, kid, subExpressionValues, false);
                    } else {
                        FindSubExpressionValues(config, kid, subExprNode, subExpressionValues, hideOutermostValue);
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
                            ignoreInitialSpace = false;
                        } else {
                            AppendNodeWithLimitedDepth(sb, kid, ref ignoreInitialSpace, unfoldToDepth - (kid.IsConceptualChild ? 1 : 0));
                        }
                    }
                }
            }

            public string ComposeToSingleString()
            {
                return ExpressionString + "\n" + string.Join("", SubExpressions.Select(sub => sub.SubExpression + ": " + sub.ValueAsString + "\n"));
            }
        }
    }
}
