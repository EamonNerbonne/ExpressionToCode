using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace ExpressionToCodeLib
{
    public struct ExpressionToCodeConfigurationValue
    {
        public ICodeAnnotator CodeAnnotator;
        public IExpressionCompiler ExpressionCompiler;
    }

    public class ExpressionToCodeConfiguration
    {
        public static readonly ExpressionToCodeConfiguration DefaultConfiguration =
            new ExpressionToCodeConfiguration(
                new ExpressionToCodeConfigurationValue {
                    CodeAnnotator = new ValuesOnStalksCodeAnnotator(),
                    ExpressionCompiler = new NormalExpressionCompiler()
                });

        public static ExpressionToCodeConfiguration CurrentConfiguration = DefaultConfiguration;

        public readonly ExpressionToCodeConfigurationValue Value;
        public ExpressionToCodeConfiguration(ExpressionToCodeConfigurationValue value) { Value = value; }
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
