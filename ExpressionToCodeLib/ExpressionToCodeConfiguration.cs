// ReSharper disable MemberCanBePrivate.Global

namespace ExpressionToCodeLib;

/// <summary>
///     Specifies details of how expressions and their values are to be formatted.  This object is immutable; all instance
///     methods are thread safe.
///     Changes to configuration return new configuration instances.
/// </summary>
public sealed record ExpressionToCodeConfiguration
{
    public ICodeAnnotator CodeAnnotator { get; init; } = CodeAnnotators.SubExpressionPerLineCodeAnnotator;
    public IExpressionCompiler ExpressionCompiler { get; init; } = ExpressionTreeCompilers.DotnetExpressionCompiler;

    [Obsolete("Replaced by UseFullyQualifiedTypeNames")]
    public IObjectStringifier ObjectStringifier
    {
        init => UseFullyQualifiedTypeNames = ((ObjectStringifyImpl)value).UseFullyQualifiedTypeNames;
    }

    public bool AlwaysUseExplicitTypeArguments { get; init; }
    public bool UseFullyQualifiedTypeNames { get; init; }
    public bool AllowVerbatimStringLiterals { get; init; } = true;

    /// <summary>
    ///     Omits builtin implicit casts (on by default).  This can cause the code to select another overload or simply fail to
    ///     compile, and the rules used to detect valid cast elision may be wrong in corner cases.
    ///     If you're purely interested in accuracy over readability of the code, you may wish to turn this off.
    /// </summary>
    public bool OmitImplicitCasts { get; init; } = true;

    public int? PrintedListLengthLimit { get; init; }
    public int? MaximumValueLength { get; init; }

    /// <summary>
    ///     The default formatter for converting an expression to code.
    /// </summary>
    public static readonly ExpressionToCodeConfiguration DefaultCodeGenConfiguration = new();

    /// <summary>
    ///     The default formatter for formatting an assertion violation.
    ///     This is identical to DefaultCodeGenConfiguration, except that long enumerables or values with long string
    ///     representations are truncated.
    /// </summary>
    public static readonly ExpressionToCodeConfiguration DefaultAssertionConfiguration = new() {
        PrintedListLengthLimit = 30,
        MaximumValueLength = 150,
    };

    /// <summary>
    ///     This configuration is used for PAssert.That(()=>...) and Expect(()=>...).  Initially
    ///     ExpressionToCodeConfiguration.DefaultAssertionConfiguration.
    ///     <para>
    ///         This field is globally mutable to allow consumers to configure the library.  If you wish to use multiple
    ///         configurations, it is recommended
    ///         to use the instance methods on a configuration instance instead of the static methods in PAssert,
    ///         ExpressionToCode and ExpressionAssertions.
    ///     </para>
    /// </summary>
    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    public static ExpressionToCodeConfiguration GlobalAssertionConfiguration = DefaultAssertionConfiguration;

    /// <summary>
    ///     This configuration is used for Expression.ToCode(() => ...) and other code-generation methods. Initially
    ///     ExpressionToCodeConfiguration.DefaultCodeGenConfiguration.
    ///     <para>
    ///         This field is globally mutable to allow consumers to configure the library.  If you wish to use multiple
    ///         configurations, it is recommended
    ///         to use the instance methods on a configuration instance instead of the static methods in PAssert,
    ///         ExpressionToCode and ExpressionAssertions.
    ///     </para>
    /// </summary>
    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    public static ExpressionToCodeConfiguration GlobalCodeGenConfiguration = DefaultCodeGenConfiguration;

    public IExpressionToCode GetExpressionToCode()
        => new ExpressionToCodeWrapper(this);

    public IAnnotatedToCode GetAnnotatedToCode()
        => new AnnotatedToCodeWrapper(this);

    sealed record AnnotatedToCodeWrapper(ExpressionToCodeConfiguration config) : IAnnotatedToCode
    {
        public string AnnotatedToCode(Expression e, string? msg, bool outerValueIsAssertionFailure)
            => config.CodeAnnotator.AnnotateExpressionTree(config, e, msg, outerValueIsAssertionFailure);
    }

    sealed record ExpressionToCodeWrapper(ExpressionToCodeConfiguration config) : IExpressionToCode
    {
        public string ToCode(Expression e)
            => ExpressionToCodeString.ToCodeString(config, e);
    }

    [Obsolete]
    public ExpressionToCodeConfiguration WithCompiler(IExpressionCompiler v)
        => this with { ExpressionCompiler = v, };

    [Obsolete]
    public ExpressionToCodeConfiguration WithAnnotator(ICodeAnnotator v)
        => this with { CodeAnnotator = v, };

    [Obsolete]
    public ExpressionToCodeConfiguration WithPrintedListLengthLimit(int? v)
        => this with { PrintedListLengthLimit = v, };

    [Obsolete]
    public ExpressionToCodeConfiguration WithObjectStringifier(IObjectStringifier withFullTypeNames)
        => this with { ObjectStringifier = withFullTypeNames, };

    [Obsolete]
    public ExpressionToCodeConfiguration WithAlwaysUseExplicitTypeArguments(bool b)
        => this with { AlwaysUseExplicitTypeArguments = b, };

    [Obsolete]
    public ExpressionToCodeConfiguration WithOmitImplicitCasts(bool b)
        => this with { OmitImplicitCasts = b, };
}

public interface ICodeAnnotator
{
    string AnnotateExpressionTree(ExpressionToCodeConfiguration config, Expression expr, string? msg, bool outerValueIsAssertionFailure);
}

public interface IExpressionCompiler
{
    Func<T> Compile<T>(Expression<Func<T>> expression);
    Delegate Compile(LambdaExpression expression);
}
