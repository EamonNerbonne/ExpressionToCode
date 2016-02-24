using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using ExpressionToCodeLib.Unstable_v2_Api;

namespace ExpressionToCodeLib
{
    public struct ExpressionToCodeConfigurationValue
    {
        public ICodeAnnotator CodeAnnotator;
        public IExpressionCompiler ExpressionCompiler;
        public IObjectToCode ObjectToCode;
        public bool AlwaysUseExplicitTypeArguments;
    }

    public class ExpressionToCodeConfiguration
    {
        public static readonly ExpressionToCodeConfiguration DefaultConfiguration =
            new ExpressionToCodeConfiguration(
                new ExpressionToCodeConfigurationValue {
                    CodeAnnotator = new ValuesOnStalksCodeAnnotator(),
                    ExpressionCompiler = new NormalExpressionCompiler(),
                    ObjectToCode = ObjectStringify.Default,
                    AlwaysUseExplicitTypeArguments = false,
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
        public ExpressionToCodeConfiguration WithObjectStringifier(IObjectToCode objectToCode) => With((ref ExpressionToCodeConfigurationValue a) => a.ObjectToCode = objectToCode);
        public ExpressionToCodeConfiguration WithAlwaysUseExplicitTypeArguments(bool alwaysUseExplicitTypeArguments) => With((ref ExpressionToCodeConfigurationValue a) => a.AlwaysUseExplicitTypeArguments = alwaysUseExplicitTypeArguments);
    }

    public interface ICodeAnnotator
    {
        string AnnotateExpressionTree(Expression expr, string msg, bool ignoreOutermostValue);
    }

    public interface IExpressionCompiler
    {
        Func<T> Compile<T>(Expression<Func<T>> expression);
    }

    public interface IObjectToCode
    {
        string PlainObjectToCode(object val, Type type);
        string TypeNameToCode(Type type);
    }
}
