using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace ExpressionToCodeLib.Unstable_v2_Api
{
    public sealed class ExpressionStringify : IExpressionToCode
    {
        public static readonly IExpressionToCode Default = new ExpressionStringify(ObjectStringify.Default, false);
        readonly IObjectToCode objectToCode;
        readonly bool explicitMethodTypeArgs;

        ExpressionStringify(IObjectToCode objectToCode, bool explicitMethodTypeArgs)
        {
            this.objectToCode = objectToCode;
            this.explicitMethodTypeArgs = explicitMethodTypeArgs;
        }

        string IExpressionToCode.ToCode(Expression e)
        {
            var sb = new StringBuilder();
            var ignoreInitialSpace = true;
            var stringifiedExpr= new ExpressionToCodeImpl(
                objectToCode,
                explicitMethodTypeArgs).ExpressionDispatch(e);
            AppendTo(sb, ref ignoreInitialSpace, stringifiedExpr);
            return sb.ToString();
        }

        static void AppendTo(StringBuilder sb, ref bool ignoreInitialSpace, StringifiedExpression node) {
            if (node.Text != null) {
                sb.Append(ignoreInitialSpace ? node.Text.TrimStart() : node.Text);
                ignoreInitialSpace = node.Text.Any() && ExpressionToCode.ShouldIgnoreSpaceAfter(node.Text[node.Text.Length - 1]);
            } else {
                foreach (var kid in node.Children)
                    AppendTo(sb, ref ignoreInitialSpace, kid);
            }
        }

        public static IExpressionToCode With(bool fullTypeNames = false, bool explicitMethodTypeArgs = false)
        {
            return new ExpressionStringify(fullTypeNames ? ObjectStringify.WithFullTypeNames : ObjectStringify.Default, explicitMethodTypeArgs);
        }
    }
}
