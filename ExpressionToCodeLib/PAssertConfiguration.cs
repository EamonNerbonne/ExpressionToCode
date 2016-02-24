using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace ExpressionToCodeLib
{
    public class PAssertConfiguration
    {
        public static readonly PAssertConfiguration DefaultConfiguration = new PAssertConfiguration(new ValuesOnStalksCodeAnnotator(), new NormalExpressionCompiler());
        public static PAssertConfiguration CurrentConfiguration = DefaultConfiguration;
        public readonly ICodeAnnotator CodeAnnotator;
        public readonly IExpressionCompiler ExpressionCompiler;
        public PAssertConfiguration(ICodeAnnotator codeAnnotator, IExpressionCompiler expressionCompiler)
        {
            CodeAnnotator = codeAnnotator;
            ExpressionCompiler = expressionCompiler;
        }
    }

    public interface ICodeAnnotator
    {
        string AnnotateExpressionTree(Expression expr, string msg, bool ignoreOutermostValue);
    }
    public interface IExpressionCompiler
    {
        Func<T> Compile<T>(Expression<Func<T>> expression);
    }

}
