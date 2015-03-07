using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace ExpressionToCodeLib.Unstable_v2_Api
{
    public sealed class ExpressionStringify : IExpressionToCode
    {
        public static readonly IExpressionToCode Default = new ExpressionStringify(ObjectToCode.Default, false);
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
            new ExpressionToCodeImpl(
                objectToCode,
                explicitMethodTypeArgs,
                (etp, depth) => {
                    sb.Append(ignoreInitialSpace ? etp.Text.TrimStart() : etp.Text);
                    ignoreInitialSpace = etp.Text.Any() && ExpressionToCode.ShouldIgnoreSpaceAfter(etp.Text[etp.Text.Length - 1]);
                }).ExpressionDispatch(e);
            return sb.ToString();
        }

        public static IExpressionToCode With(bool fullTypeNames = false, bool explicitMethodTypeArgs = false)
        {
            return new ExpressionStringify(fullTypeNames ? ObjectToCode.WithFullTypeNames : ObjectToCode.Default, explicitMethodTypeArgs);
        }
    }
}