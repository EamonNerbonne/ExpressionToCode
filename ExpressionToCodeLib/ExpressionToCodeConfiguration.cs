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
        internal delegate void WithDelegate(ref ExpressionToCodeConfigurationValue configToEdit);
        internal ExpressionToCodeConfigurationValue With(WithDelegate edit)
        {
            var configCopy = this;
            edit(ref configCopy);
            return configCopy;
        }

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

        delegate void WithDelegate(ref ExpressionToCodeConfigurationValue configToEdit);
        ExpressionToCodeConfiguration With(WithDelegate edit)
        {
            var configCopy = Value;
            edit(ref configCopy);
            return new ExpressionToCodeConfiguration(configCopy);
        }


        public ExpressionToCodeConfiguration WithCompiler(IExpressionCompiler compiler) => With((ref ExpressionToCodeConfigurationValue a) => a.ExpressionCompiler = compiler);
        public ExpressionToCodeConfiguration WithAnnotator(ICodeAnnotator annotator) => With((ref ExpressionToCodeConfigurationValue a) => a.CodeAnnotator = annotator);
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
