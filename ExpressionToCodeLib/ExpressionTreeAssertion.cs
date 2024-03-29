namespace ExpressionToCodeLib;

public static class ExpressionTreeAssertion
{
    public static void Assert(this ExpressionToCodeConfiguration config, Expression<Func<bool>> assertion, string? msg = null)
    {
        var compiled = config.ExpressionCompiler.Compile(assertion);
        bool ok;
        try {
            ok = compiled();
        } catch (Exception e) {
            throw new InvalidOperationException(config.CodeAnnotator.AnnotateExpressionTree(config, assertion.Body, "evaluating assertion aborted due to exception: " + msg, true), e);
        }

        if (!ok) {
            throw new InvalidOperationException(config.CodeAnnotator.AnnotateExpressionTree(config, assertion.Body, msg ?? "assertion failed", true), null);
        }
    }
}
