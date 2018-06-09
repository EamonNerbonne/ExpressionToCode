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
            => (msg == null ? "" : msg + "\n\n") + ExpressionWithSubExpressions.Create(config, expr, hideOutermostValue).ComposeToSingleString();

        struct ExpressionWithSubExpressions
        {
            string ExpressionString;
            SubExpressionValue[] SubExpressions;

            public struct SubExpressionValue : IEquatable<SubExpressionValue>
            {
                public string SubExpression,
                    ValueAsString;

                public override int GetHashCode()
                    => SubExpression.GetHashCode() + 37 * ValueAsString.GetHashCode();

                public override bool Equals(object obj)
                    => obj is SubExpressionValue val && Equals(val);

                public bool Equals(SubExpressionValue val)
                    => SubExpression == val.SubExpression && ValueAsString == val.ValueAsString;
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
                return new ExpressionWithSubExpressions { ExpressionString = fullExprText, SubExpressions = subExpressionValues.Distinct().ToArray() };
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
                    var valueString = ObjectToCodeImpl.ExpressionValueAsCode(config, node.OptionalValue, 10);
                    AppendNodeToStringBuilder(sb, subExprNode, ref ignoreInitialSpace);
                    var maxSize = 80;
                    var subExprString = sb.Length <= maxSize
                        ? sb.ToString()
                        : sb.ToString(0, maxSize / 2 - 1) + "  …  " + sb.ToString(sb.Length - (maxSize / 2 - 1), maxSize / 2 - 1);
                    if (!string.IsNullOrEmpty(valueString)) {
                        subExpressionValues.Add(new SubExpressionValue { SubExpression = subExprString, ValueAsString = valueString });
                    }
                }

                foreach (var kid in node.Children) {
                    if (kid.IsConceptualChild) {
                        FindSubExpressionValues(config, kid, kid, subExpressionValues, false);
                    }
                }

                foreach (var kid in node.Children) {
                    if (!kid.IsConceptualChild) {
                        FindSubExpressionValues(config, kid, subExprNode, subExpressionValues, hideOutermostValue);
                    }
                }
            }

            public string ComposeToSingleString()
            {
                var maxExprLen = SubExpressions.Max(sub => (int?)sub.SubExpression.Length) ?? 0;

                if (maxExprLen < 30) {
                    return ExpressionString + "\n" + string.Join(
                        "",
                        SubExpressions.Select(sub => sub.SubExpression.PadLeft(maxExprLen) + "   →   " + sub.ValueAsString + "\n"));
                }

                return ExpressionString + "\n" + string.Join("", SubExpressions.Select(sub => sub.SubExpression + "\n     →   " + sub.ValueAsString + "\n"));
            }
        }
    }
}
