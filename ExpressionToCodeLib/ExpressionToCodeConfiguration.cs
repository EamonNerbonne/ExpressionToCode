using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using ExpressionToCodeLib.Internal;

namespace ExpressionToCodeLib
{
    struct ExpressionToCodeConfigurationValue
    {
        public ICodeAnnotator CodeAnnotator;
        public IExpressionCompiler ExpressionCompiler;
        public IObjectStringifier ObjectStringifier;
        public bool AlwaysUseExplicitTypeArguments;
    }

    public class ExpressionToCodeConfiguration
    {
        public static readonly ExpressionToCodeConfiguration DefaultConfiguration =
            new ExpressionToCodeConfiguration(
                new ExpressionToCodeConfigurationValue {
                    CodeAnnotator = CodeAnnotators.ValuesOnStalksCodeAnnotator,
                    ExpressionCompiler = ExpressionTreeCompilers.DefaultExpressionCompiler,
                    ObjectStringifier = ObjectStringify.Default,
                    AlwaysUseExplicitTypeArguments = false,
                });

        public static ExpressionToCodeConfiguration CurrentConfiguration = DefaultConfiguration;
        internal readonly ExpressionToCodeConfigurationValue Value;
        ExpressionToCodeConfiguration(ExpressionToCodeConfigurationValue value) { Value = value; }

        delegate void WithDelegate(ref ExpressionToCodeConfigurationValue configToEdit);

        ExpressionToCodeConfiguration With(WithDelegate edit)
        {
            var configCopy = Value;
            edit(ref configCopy);
            return new ExpressionToCodeConfiguration(configCopy);
        }

        public ExpressionToCodeConfiguration WithCompiler(IExpressionCompiler compiler) => With((ref ExpressionToCodeConfigurationValue a) => a.ExpressionCompiler = compiler);
        public ExpressionToCodeConfiguration WithAnnotator(ICodeAnnotator annotator) => With((ref ExpressionToCodeConfigurationValue a) => a.CodeAnnotator = annotator);

        public ExpressionToCodeConfiguration WithObjectStringifier(IObjectStringifier objectStringifier)
            => With((ref ExpressionToCodeConfigurationValue a) => a.ObjectStringifier = objectStringifier);

        public ExpressionToCodeConfiguration WithAlwaysUseExplicitTypeArguments(bool alwaysUseExplicitTypeArguments)
            => With((ref ExpressionToCodeConfigurationValue a) => a.AlwaysUseExplicitTypeArguments = alwaysUseExplicitTypeArguments);

        public IExpressionToCode GetExpressionToCode() => new ExpressionToCodeWrapper(this);
        public IAnnotatedToCode GetAnnotatedToCode() => new AnnotatedToCodeWrapper(this);

        class AnnotatedToCodeWrapper : IAnnotatedToCode
        {
            readonly ExpressionToCodeConfiguration config;
            public AnnotatedToCodeWrapper(ExpressionToCodeConfiguration config) { this.config = config; }

            public string AnnotatedToCode(Expression e, string msg, bool hideOutermostValue)
                => config.Value.CodeAnnotator.AnnotateExpressionTree(config, e, msg, hideOutermostValue);
        }

        sealed class ExpressionToCodeWrapper : IExpressionToCode
        {
            readonly ExpressionToCodeConfiguration config;
            public ExpressionToCodeWrapper(ExpressionToCodeConfiguration config) { this.config = config; }
            public string ToCode(Expression e) => ExpressionToCodeString.ToCodeString(config, e);
        }
    }

    public interface ICodeAnnotator
    {
        string AnnotateExpressionTree(ExpressionToCodeConfiguration config, Expression expr, string msg, bool hideOutermostValue);
    }

    public interface IExpressionCompiler
    {
        Func<T> Compile<T>(Expression<Func<T>> expression);
    }
}
