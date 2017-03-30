using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using ExpressionToCodeLib.Internal;

namespace ExpressionToCodeLib
{
    struct ExpressionToCodeConfigurationValue
    {
        public ICodeAnnotator CodeAnnotator;
        public IExpressionCompiler ExpressionCompiler;
        public IObjectStringifier ObjectStringifier;
        public bool AlwaysUseExplicitTypeArguments;
        public int? PrintedListLengthLimit;
        public int? MaximumValueLength;
    }

    /// <summary>
    /// Specifies details of how expressions and their values are to be formatted.  This object is immutable; all instance methods are thread safe.
    /// Changes to configuration return new configuration instances.
    /// </summary>
    public class ExpressionToCodeConfiguration
    {
        /// <summary>
        /// The default formatter for converting an expression to code. Defaults are:
        /// <para>- Avoid generic type parameters in output where they can be inferred.</para>
        /// <para>- Omit namespaces from type names (as opposed to fully qualifying type names)</para>
        /// <para>- Use the default .net expression compiler (as opposed to the experimental optimized compiler).</para>
        /// <para>- Annotate values using "stalks" hanging under expressions.</para>
        /// <para>- Print all elements in an enumerable (this will cause crashes on infinite or very large enumerables).</para>
        /// </summary>
        public static readonly ExpressionToCodeConfiguration DefaultCodeGenConfiguration =
            new ExpressionToCodeConfiguration(
                new ExpressionToCodeConfigurationValue {
                    CodeAnnotator = CodeAnnotators.ValuesOnStalksCodeAnnotator,
                    ExpressionCompiler = ExpressionTreeCompilers.DefaultExpressionCompiler,
                    ObjectStringifier = ObjectStringify.Default,
                    AlwaysUseExplicitTypeArguments = false,
                });

        /// <summary>
        /// The default formatter for formatting an assertion violation.
        /// This is identical to DefaultCodeGenConfiguration, except that enumerable contents after the first 10 elements are elided.
        /// </summary>
        public static readonly ExpressionToCodeConfiguration DefaultAssertionConfiguration =
            DefaultCodeGenConfiguration.WithPrintedListLengthLimit(10).WithMaximumValueLength(80);

        /// <summary>
        /// This configuration is used for PAssert.That(()=>...) and Expect(()=>...).  Initially ExpressionToCodeConfiguration.DefaultAssertionConfiguration.
        /// 
        /// <para>This field is globally mutable to allow consumers to configure the library.  If you wish to use multiple configurations, it is recommended
        /// to use the instance methods on a configuration instance instead of the static methods in PAssert, ExpressionToCode and ExpressionAssertions.</para>
        /// </summary>
        public static ExpressionToCodeConfiguration GlobalAssertionConfiguration = DefaultAssertionConfiguration;

        /// <summary>
        /// This configuration is used for Expression.ToCode(() => ...) and other code-generation methods. Initially ExpressionToCodeConfiguration.DefaultCodeGenConfiguration.
        /// 
        /// <para>This field is globally mutable to allow consumers to configure the library.  If you wish to use multiple configurations, it is recommended
        /// to use the instance methods on a configuration instance instead of the static methods in PAssert, ExpressionToCode and ExpressionAssertions.</para>
        /// </summary>
        public static ExpressionToCodeConfiguration GlobalCodeGenConfiguration = DefaultCodeGenConfiguration;

        internal readonly ExpressionToCodeConfigurationValue Value;
        ExpressionToCodeConfiguration(ExpressionToCodeConfigurationValue value) => Value = value;

        delegate void WithDelegate(ref ExpressionToCodeConfigurationValue configToEdit);

        ExpressionToCodeConfiguration With(WithDelegate edit)
        {
            var configCopy = Value;
            edit(ref configCopy);
            return new ExpressionToCodeConfiguration(configCopy);
        }

        public ExpressionToCodeConfiguration WithCompiler(IExpressionCompiler compiler) => With((ref ExpressionToCodeConfigurationValue a) => a.ExpressionCompiler = compiler);
        public ExpressionToCodeConfiguration WithAnnotator(ICodeAnnotator annotator) => With((ref ExpressionToCodeConfigurationValue a) => a.CodeAnnotator = annotator);
        public ExpressionToCodeConfiguration WithPrintedListLengthLimit(int? limitListsToLength) => With((ref ExpressionToCodeConfigurationValue a) => a.PrintedListLengthLimit = limitListsToLength);
        public ExpressionToCodeConfiguration WithMaximumValueLength(int? limitValueStringsToLength) => With((ref ExpressionToCodeConfigurationValue a) => a.MaximumValueLength = limitValueStringsToLength);

        public ExpressionToCodeConfiguration WithObjectStringifier(IObjectStringifier objectStringifier)
            => With((ref ExpressionToCodeConfigurationValue a) => a.ObjectStringifier = objectStringifier);

        public ExpressionToCodeConfiguration WithAlwaysUseExplicitTypeArguments(bool alwaysUseExplicitTypeArguments)
            => With((ref ExpressionToCodeConfigurationValue a) => a.AlwaysUseExplicitTypeArguments = alwaysUseExplicitTypeArguments);

        public IExpressionToCode GetExpressionToCode() => new ExpressionToCodeWrapper(this);
        public IAnnotatedToCode GetAnnotatedToCode() => new AnnotatedToCodeWrapper(this);

        class AnnotatedToCodeWrapper : IAnnotatedToCode
        {
            readonly ExpressionToCodeConfiguration config;
            public AnnotatedToCodeWrapper(ExpressionToCodeConfiguration config) => this.config = config;

            public string AnnotatedToCode(Expression e, string msg, bool hideOutermostValue)
                => config.Value.CodeAnnotator.AnnotateExpressionTree(config, e, msg, hideOutermostValue);
        }

        sealed class ExpressionToCodeWrapper : IExpressionToCode
        {
            readonly ExpressionToCodeConfiguration config;
            public ExpressionToCodeWrapper(ExpressionToCodeConfiguration config) => this.config = config;
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
