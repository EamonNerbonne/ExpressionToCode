using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace ExpressionToCodeLib
{
    internal class ValuesOnStalksCodeAnnotator : ICodeAnnotator
    {
        public string AnnotateExpressionTree(Expression expr, string msg, bool hideOutermostValue)
        {
            var splitLine = ExpressionToStringWithValues(expr, hideOutermostValue);

            var exprWithStalkedValues = new StringBuilder();
            if (msg == null) {
                exprWithStalkedValues.AppendLine(splitLine.Line);
            } else if (IsMultiline(msg)) {
                exprWithStalkedValues.AppendLine(msg);
                exprWithStalkedValues.AppendLine(splitLine.Line);
            } else {
                exprWithStalkedValues.AppendLine(splitLine.Line + "  :  " + msg);
            }

            for (int nodeI = splitLine.Nodes.Length - 1; nodeI >= 0; nodeI--) {
                char[] stalkLine = new string('\u2007', splitLine.Nodes[nodeI].Location).ToCharArray(); //figure-spaces.
                for (int i = 0; i < stalkLine.Length; i++) {
                    if (splitLine.Line[i] == ' ') {
                        stalkLine[i] = ' '; //use normal spaces where the expr used normal spaces for more natural spacing.
                    }
                }

                for (int prevI = 0; prevI < nodeI; prevI++) {
                    stalkLine[splitLine.Nodes[prevI].Location] = '\u2502'; //light vertical lines
                }
                exprWithStalkedValues.AppendLine((new string(stalkLine) + splitLine.Nodes[nodeI].Value).TrimEnd());
            }

            return exprWithStalkedValues.ToString();
        }

        static bool IsMultiline(string msg)
        {
            var idxAfterNewline = msg.IndexOf('\n') + 1;
            return idxAfterNewline > 0 && idxAfterNewline < msg.Length;
        }

        static SplitExpressionLine ExpressionToStringWithValues(Expression e, bool hideOutermostValue)
        {
            var nodeInfos = new List<SubExpressionInfo>();
            var sb = new StringBuilder();
            bool ignoreInitialSpace = true;
            var node = new ExpressionToCodeImpl().ExpressionDispatch(e);
            AppendTo(sb, nodeInfos, node, ref ignoreInitialSpace, !hideOutermostValue);
            nodeInfos.Add(new SubExpressionInfo { Location = sb.Length, Value = null });
            return new SplitExpressionLine { Line = sb.ToString().TrimEnd(), Nodes = nodeInfos.ToArray() };
        }

        static void AppendTo(StringBuilder sb, List<SubExpressionInfo> nodeInfos, StringifiedExpression node, ref bool ignoreInitialSpace, bool showTopExpressionValue)
        {
            if (node.Text != null) {
                var trimmedText = ignoreInitialSpace ? node.Text.TrimStart() : node.Text;
                var pos0 = sb.Length;
                sb.Append(trimmedText);
                ignoreInitialSpace = node.Text.Any() && ExpressionToCode.ShouldIgnoreSpaceAfter(node.Text[node.Text.Length - 1]);
                if (showTopExpressionValue) {
                    string valueString = node.OptionalValue == null ? null : ObjectToCode.ExpressionValueAsCode(node.OptionalValue);
                    if (valueString != null) {
                        nodeInfos.Add(new SubExpressionInfo { Location = pos0 + trimmedText.Length / 2, Value = valueString });
                    }
                }
            }
            foreach (var kid in node.Children) {
                AppendTo(sb, nodeInfos, kid, ref ignoreInitialSpace, showTopExpressionValue || kid.IsConceptualChild);
            }
        }

        struct SplitExpressionLine
        {
            public string Line;
            public SubExpressionInfo[] Nodes;
        }

        struct SubExpressionInfo
        {
            public int Location;
            public string Value;
        }
    }
}
