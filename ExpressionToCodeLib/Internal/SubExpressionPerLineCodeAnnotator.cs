namespace ExpressionToCodeLib.Internal;

class SubExpressionPerLineCodeAnnotator : ICodeAnnotator
{
    public string AnnotateExpressionTree(ExpressionToCodeConfiguration config, Expression expr, string? msg, bool outerValueIsAssertionFailure)
        => (msg == null ? "" : msg + "\n\n") + ExpressionWithSubExpressions.Create(config, expr, outerValueIsAssertionFailure).ComposeToSingleString();

    struct ExpressionWithSubExpressions
    {
        const string spacedArrow = "   →   ";
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

        public static ExpressionWithSubExpressions Create(ExpressionToCodeConfiguration config, Expression e, bool outerValueIsAssertionFailure)
        {
            var sb = new StringBuilder();
            var ignoreInitialSpace = true;
            var node = new ExpressionToCodeImpl(config).ExpressionDispatch(e);
            AppendNodeToStringBuilder(sb, node, ref ignoreInitialSpace);
            var fullExprText = sb.ToString();
            var subExpressionValues = new List<SubExpressionValue>();
            FindSubExpressionValues(config, node, node, subExpressionValues, outerValueIsAssertionFailure);
            var assertionValue = outerValueIsAssertionFailure ? OutermostValue(config, node) : null;
            return new ExpressionWithSubExpressions {
                ExpressionString = fullExprText
                    + (assertionValue != null ? "\n" + spacedArrow + assertionValue + " (caused assertion failure)\n" : ""),
                SubExpressions = subExpressionValues.Distinct().ToArray()
            };
        }

        static string? OutermostValue(ExpressionToCodeConfiguration config, StringifiedExpression node)
        {
            if (node.OptionalValue != null) {
                return ObjectToCodeImpl.ExpressionValueAsCode(config, node.OptionalValue, 10);
            }
            foreach (var kid in node.Children) {
                if (!kid.IsConceptualChild) {
                    var value = OutermostValue(config, kid);
                    if (value != null) {
                        return value;
                    }
                }
            }
            foreach (var kid in node.Children) {
                if (kid.IsConceptualChild) {
                    var value = OutermostValue(config, kid);
                    if (value != null) {
                        return value;
                    }
                }
            }
            return null;
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
            bool outerValueIsAssertionFailure)
        {
            if (!outerValueIsAssertionFailure && node.OptionalValue != null) {
                var sb = new StringBuilder();
                var ignoreInitialSpace = true;
                var valueString = ObjectToCodeImpl.ExpressionValueAsCode(config, node.OptionalValue, 10);
                AppendNodeToStringBuilder(sb, subExprNode, ref ignoreInitialSpace);
                var maxSize = Math.Max(40, config.Value.MaximumValueLength ?? 200);
                var subExprString = sb.Length <= maxSize
                    ? sb.ToString()
                    : sb.ToString(0, maxSize / 2 - 1) + "  …  " + sb.ToString(sb.Length - (maxSize / 2 - 1), maxSize / 2 - 1);
                // ReSharper disable once ReplaceWithStringIsNullOrEmpty - for nullability analysis
                if (valueString != null && valueString != "") {
                    subExpressionValues.Add(new SubExpressionValue { SubExpression = subExprString, ValueAsString = valueString });
                }
            }

            foreach (var kid in node.Children) {
                if (!kid.IsConceptualChild) {
                    FindSubExpressionValues(config, kid, subExprNode, subExpressionValues, outerValueIsAssertionFailure);
                }
            }

            foreach (var kid in node.Children) {
                if (kid.IsConceptualChild) {
                    FindSubExpressionValues(config, kid, kid, subExpressionValues, false);
                }
            }
        }

        public string ComposeToSingleString()
        {
            var maxLineLength = SubExpressions.Max(sub => sub.SubExpression.Length + spacedArrow.Length + sub.ValueAsString.Length as int?) ?? 0;
            var maxExprLength = SubExpressions.Max(sub => sub.SubExpression.Length as int?) ?? 0;
            var containsANewline = SubExpressions.Any(sub => sub.SubExpression.Contains("\n") || sub.ValueAsString.Contains("\n"));

            return ExpressionString + "\n"
                + string.Join(
                    "",
                    maxLineLength <= 80 && maxExprLength <= 30 && !containsANewline
                        ? SubExpressions.Select(sub => sub.SubExpression.PadLeft(maxExprLength) + spacedArrow + sub.ValueAsString + "\n")
                        : SubExpressions.Select(sub => sub.SubExpression + "\n  " + spacedArrow + sub.ValueAsString + "\n\n")
                );
        }
    }
}
