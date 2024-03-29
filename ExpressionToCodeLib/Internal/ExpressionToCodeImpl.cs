namespace ExpressionToCodeLib.Internal;

class ExpressionToCodeImpl : IExpressionTypeDispatch<StringifiedExpression>
{
    #region General Helpers
    public ExpressionToCodeImpl(ExpressionToCodeConfiguration config)
        => this.config = config;

    readonly ExpressionToCodeConfiguration config;

    string? PlainObjectToCode(object? val, Type? type)
        => ObjectToCodeImpl.PlainObjectToCode(config, val, type);

    string TypeNameToCode(Type type)
        => type.ToCSharpFriendlyTypeName(config, false);

    bool AlwaysUseExplicitTypeArguments => config.AlwaysUseExplicitTypeArguments;

    [Pure]
    IEnumerable<StringifiedExpression> NestExpression(ExpressionType? parentType, Expression child, bool parensIfEqualRank = false)
    {
        var parentRank = parentType == null ? 0 : ExpressionPrecedence.Rank(parentType.Value);
        var needsParens = parentRank > 0
            && (parensIfEqualRank ? parentRank - 1 : parentRank) < ExpressionPrecedence.Rank(child.NodeType);

        if (needsParens) {
            yield return StringifiedExpression.TextOnly("(");
        }

        yield return SingleChildDispatch(child);

        if (needsParens) {
            yield return StringifiedExpression.TextOnly(")");
        }
    }

    StringifiedExpression SingleChildDispatch(Expression child)
        => this.ExpressionDispatch(child).MarkAsConceptualChild();

    StringifiedExpression SingleChildDispatch(string? prefix, Expression child)
        => StringifiedExpression.WithChildren(new[] { StringifiedExpression.TextOnly(prefix), this.ExpressionDispatch(child), }).MarkAsConceptualChild();

    [Pure]
    static IEnumerable<StringifiedExpression> JoinDispatch(IEnumerable<StringifiedExpression> children, string joiner)
    {
        var isFirst = true;
        foreach (var child in children) {
            if (!isFirst) {
                yield return StringifiedExpression.TextOnly(joiner);
            }

            yield return child;

            isFirst = false;
        }
    }

    [Pure]
    static IEnumerable<StringifiedExpression> ArgListDispatch(
        IEnumerable<StringifiedExpression> arguments,
        Expression? value = null,
        string open = "(",
        string close = ")",
        string joiner = ", ")
    {
        yield return value != null ? StringifiedExpression.TextAndExpr(open, value) : StringifiedExpression.TextOnly(open);

        foreach (var o in JoinDispatch(arguments, joiner)) {
            yield return o;
        }

        yield return StringifiedExpression.TextOnly(close);
    }

    readonly struct KidsBuilder
    {
        readonly List<StringifiedExpression> kids;

        KidsBuilder(List<StringifiedExpression> init)
            => kids = init;

        [Pure]
        public static KidsBuilder Create()
            => new(new());

        public void Add(StringifiedExpression node)
            => kids.Add(node);

        public void Add(IEnumerable<StringifiedExpression> nodes)
            => kids.AddRange(nodes);

        public void Add(string text, Expression value)
            => kids.Add(StringifiedExpression.TextAndExpr(text, value));

        public void Add(string text)
            => kids.Add(StringifiedExpression.TextOnly(text));

        public StringifiedExpression Finish()
            => StringifiedExpression.WithChildren(kids.ToArray());
    }

    [Pure]
    StringifiedExpression BinaryDispatch(string op, Expression e)
    {
        var kids = KidsBuilder.Create();
        var be = (BinaryExpression)e;
        UnwrapEnumOp(be, out var left, out var right);
        kids.Add(NestExpression(be.NodeType, left));
        kids.Add(" " + op + " ", e);
        kids.Add(NestExpression(be.NodeType, right, true));
        return kids.Finish();
    }

    [Pure]
    static void UnwrapEnumOp(BinaryExpression be, out Expression left, out Expression right)
    {
        left = be.Left;
        right = be.Right;
        var uncastLeft = left.NodeType == ExpressionType.Convert ? ((UnaryExpression)left).Operand : null;
        var uncastRight = right.NodeType == ExpressionType.Convert ? ((UnaryExpression)right).Operand : null;
        if (uncastLeft != null) {
            if (uncastRight != null) {
                if (uncastRight.Type.EnsureNullability() == uncastLeft.Type.EnsureNullability()) {
                    left = uncastLeft;
                    right = uncastRight;
                }
            } else {
                UnwrapEnumBinOp(uncastLeft, ref left, ref right);
            }
        } else if (uncastRight != null) {
            //implies uleft == null
            UnwrapEnumBinOp(uncastRight, ref right, ref left);
        }
    }

    static void UnwrapEnumBinOp(Expression expr1uncast, ref Expression expr1, ref Expression expr2)
    {
        var expr1nonnullableType = expr1uncast.Type.AvoidNullability();
        var expr2nonnullableType = expr2.Type.AvoidNullability();
        if (expr1nonnullableType.GetTypeInfo().IsEnum
            && expr1nonnullableType.GetTypeInfo().GetEnumUnderlyingType() == expr2nonnullableType
            || expr1nonnullableType == typeof(char) && expr2nonnullableType == typeof(int)
        ) {
            expr1 = expr1uncast;

            expr2 = expr2 is not ConstantExpression { NodeType: ExpressionType.Constant, } constantExpression
                ? Expression.Convert(expr2, expr1uncast.Type)
                : constantExpression.Value == null
                    ? Expression.Default(expr1uncast.Type.EnsureNullability())
                    : expr1nonnullableType == typeof(char)
                        ? Expression.Constant((char)(int)constantExpression.Value)
                        : Expression.Constant(Enum.ToObject(expr1nonnullableType, constantExpression.Value));
        }
    }

    [Pure]
    StringifiedExpression UnaryDispatch(string op, Expression e)
    {
        var ue = (UnaryExpression)e;
        var needsSpace = ExpressionPrecedence.TokenizerConfusable(ue.NodeType, ue.Operand.NodeType);
        var kids = KidsBuilder.Create();
        kids.Add(op + (needsSpace ? " " : ""), e);
        kids.Add(NestExpression(ue.NodeType, ue.Operand));
        return kids.Finish();
    }

    [Pure]
    StringifiedExpression UnaryDispatchConvert(Expression e)
    {
        var kids = KidsBuilder.Create();
        var ue = (UnaryExpression)e;
        var operand = ue.Operand;
        if (!config.OmitImplicitCasts
            || !ReflectionHelpers.CanImplicitlyCast(operand.Type, e.Type)
            || typeof(Delegate).IsAssignableFrom(operand.Type) && operand.NodeType == ExpressionType.Lambda
            || typeof(Expression).IsAssignableFrom(operand.Type) && operand.NodeType == ExpressionType.Quote
        ) {
            if (e.Type.GetTypeInfo().IsAssignableFrom(operand.Type)) // base class, basically; don't re-print identical values.
            {
                kids.Add("(" + TypeNameToCode(e.Type) + ")");
            } else {
                kids.Add("(" + TypeNameToCode(e.Type) + ")", e);
            }

            if (operand.NodeType == ExpressionType.Convert) {
                var nestedConvert = (UnaryExpression)operand;
                kids.Add("(" + TypeNameToCode(operand.Type) + ")");
                kids.Add(NestExpression(nestedConvert.NodeType, nestedConvert.Operand));
            } else {
                kids.Add(NestExpression(ue.NodeType, operand));
            }
        } else {
            kids.Add(NestExpression(ue.NodeType, operand));
        }

        return kids.Finish();
    }

    [Pure]
    StringifiedExpression UnaryPostfixDispatch(string op, Expression e)
    {
        var kids = KidsBuilder.Create();
        var ue = (UnaryExpression)e;
        kids.Add(NestExpression(ue.NodeType, ue.Operand));
        kids.Add(op, e);
        return kids.Finish();
    }

    [Pure]
    StringifiedExpression TypeOpDispatch(string op, Expression e)
    {
        var kids = KidsBuilder.Create();
        kids.Add(NestExpression(e.NodeType, ((TypeBinaryExpression)e).Expression));
        kids.Add(" " + op + " ", e);
        kids.Add(TypeNameToCode(((TypeBinaryExpression)e).TypeOperand));
        return kids.Finish();
    }

    [Pure]
    StringifiedExpression StatementDispatch(Expression e, ExpressionType? parentType = null)
    {
        var kids = KidsBuilder.Create();
        kids.Add(NestExpression(parentType, e));
        kids.Add("; ");
        return kids.Finish();
    }

    [Pure]
    StringifiedExpression StatementDispatch(string prefix, Expression e, ExpressionType? parentType = null)
    {
        var kids = KidsBuilder.Create();
        kids.Add(prefix);
        kids.Add(" ");
        kids.Add(StatementDispatch(e, parentType));
        return kids.Finish();
    }
    #endregion

    #region Hard Cases
    [Pure]
    public StringifiedExpression DispatchLambda(Expression e)
    {
        var kids = KidsBuilder.Create();
        var le = (LambdaExpression)e;
        kids.Add(
            le.Parameters.Count == 1
                ? NestExpression(e.NodeType, le.Parameters.Single())
                : ArgListDispatch(le.Parameters.Select(SingleChildDispatch))
            //though delegate lambdas do support ref/out parameters, expression tree lambda's don't
        );
        kids.Add(" => ");
        kids.Add(NestExpression(le.NodeType, le.Body));
        return kids.Finish();
    }

    static bool IsThisRef(Expression e)
        => e is ConstantExpression { Value: not null, }
            && e.Type.GuessTypeClass() == ReflectionHelpers.TypeClass.NormalType;

    static bool IsClosureRef(Expression e)
        => e is ConstantExpression { Value: not null, } or { NodeType: ExpressionType.MemberAccess, }
            && e.Type.GuessTypeClass() is ReflectionHelpers.TypeClass.ClosureType or ReflectionHelpers.TypeClass.TopLevelProgramClosureType;

    [Pure]
    public StringifiedExpression DispatchMemberAccess(Expression e)
    {
        var kids = KidsBuilder.Create();
        var me = (MemberExpression)e;
        var memberOfExpr = me.Expression;
        if (memberOfExpr != null && !IsThisRef(memberOfExpr) && !IsClosureRef(memberOfExpr)) {
            kids.Add(NestExpression(e.NodeType, memberOfExpr));
            kids.Add(".");
        } else if (ReflectionHelpers.IsMemberInfoStatic(me.Member)) {
            kids.Add(TypeNameToCode(me.Member.DeclaringType ?? throw new("A static member must have a declaring type")) + ".");
        }

        kids.Add(me.Member.Name, e);
        return kids.Finish();
    }

    static readonly MethodInfo createDelegate = ((Func<Type, object, MethodInfo, Delegate>)Delegate.CreateDelegate).Method;

    [Pure]
    public StringifiedExpression DispatchCall(Expression e)
    {
        var kids = KidsBuilder.Create();

        var mce = (MethodCallExpression)e;

        var optPropertyInfo = ReflectionHelpers.GetPropertyIfGetter(mce.Method);
        if (optPropertyInfo != null
            && mce.Object != null
            && (optPropertyInfo.Name == "Item"
                || optPropertyInfo.Name == "Chars" && mce.Object.Type == typeof(string))) {
            kids.Add(NestExpression(mce.NodeType, mce.Object));
            //indexers don't support ref/out; so we can use unprefixed arguments
            kids.Add(ArgListDispatch(GetArgumentsForMethod(mce.Method, mce.Arguments), mce, "[", "]"));
        } else if (mce.Method.Equals(createDelegate)
            && mce.Arguments.Count == 3
            && mce.Arguments[2] is ConstantExpression { Value: MethodInfo implicitlyConstructedDelegateMethodTarget, }) {
            //.net 4.0
            //implicitly constructed delegate from method group.
            var targetExpr = mce.Arguments[1] is ConstantExpression { Value: null, } ? null : mce.Arguments[1];
            kids.Add(StringifyMethodName(mce, implicitlyConstructedDelegateMethodTarget, targetExpr));
        } else if (mce.Method.Name == "CreateDelegate"
            && mce.Arguments.Count == 2
            && mce.Object is ConstantExpression { NodeType : ExpressionType.Constant, Value: MethodInfo targetMethod, }
            && mce.Object.Type == typeof(MethodInfo)
            && mce.Method.GetParameters()[1].ParameterType == typeof(object)
        ) {
            //.net 4.5
            //implicitly constructed delegate from method group.
            var targetExpr = mce.Arguments[1] is ConstantExpression { NodeType: ExpressionType.Constant, Value: null, }
                ? null
                : mce.Arguments[1];
            kids.Add(StringifyMethodName(mce, targetMethod, targetExpr));
        } else if (mce.Object == null
            && mce.Type.FullName == "System.FormattableString"
            && mce.Method.DeclaringType?.FullName == "System.Runtime.CompilerServices.FormattableStringFactory"
            && mce.Method.Name == "Create"
            && mce.Arguments.Count == 2
            && mce.Arguments[0] is ConstantExpression { Value: string formattableStringFormatString, }
            && mce.Arguments[1] is NewArrayExpression { Expressions: { } formattableStringArguments, }
        ) {
            //.net 4.6
            //string-interpolations are compiled into FormattableStringFactory.Create
            kids = AddStringInterpolation(kids, formattableStringFormatString, formattableStringArguments);
        } else if (mce.Object == null
            && mce.Type == typeof(string)
            && mce.Method.DeclaringType == typeof(string)
            && mce.Method.Name == "Format"
            && mce.Arguments.Count == 2
            && mce.Arguments[0].Type == typeof(string)
            && mce.Arguments[0] is ConstantExpression { Value: string stringInterpolationFormatString, }
            && mce.Arguments[1].Type == typeof(object[])
            && mce.Arguments[1] is NewArrayExpression { Expressions: { } stringInterpolationArguments, }
        ) {
            //.net 4.6
            //string-interpolations are compiled into FormattableStringFactory.Create
            kids = AddStringInterpolation(kids, stringInterpolationFormatString, stringInterpolationArguments);
        } else if (mce.Object == null
            && mce.Type == typeof(string)
            && mce.Method.DeclaringType == typeof(string)
            && mce.Method.Name == "Format"
            && mce.Arguments.Count >= 2
            && mce.Arguments[0].Type == typeof(string)
            && mce.Arguments[0] is ConstantExpression { Value: string stringInterpolationFormatString2, }
            && mce.Method.GetParameters().Skip(1).All(pi => pi.ParameterType == typeof(object))
        ) {
            //.net 4.6
            //string-interpolations are compiled into FormattableStringFactory.Create
            kids = AddStringInterpolation(kids, stringInterpolationFormatString2, mce.Arguments.Skip(1));
        } else if (mce.Object != null && mce.Method.Attributes.HasFlag(MethodAttributes.SpecialName) && mce.Method.Name == "get_Item") {
            //.net 4.5.1 or older object indexer.

            kids.Add(NestExpression(mce.NodeType, mce.Object));
            kids.Add(ArgListDispatch(mce.Arguments.Select(SingleChildDispatch), mce, "[", "]"));
            return kids.Finish();
        } else {
            var isExtensionMethod = mce.Method.IsStatic
                && mce.Method.GetCustomAttributes(typeof(ExtensionAttribute), false).Any() && mce.Arguments.Any()
                && mce.Object == null;
            var objectExpr = isExtensionMethod ? mce.Arguments.First() : mce.Object;
            kids.Add(StringifyMethodName(mce, mce.Method, objectExpr));
            var args = GetArgumentsForMethod(mce.Method, mce.Arguments);

            kids.Add(ArgListDispatch(isExtensionMethod ? args.Skip(1) : args));
        }

        return kids.Finish();
    }

    KidsBuilder AddStringInterpolation(KidsBuilder kids, string formatString, IEnumerable<Expression> arguments)
    {
        var interpolationArgumentsStringified = arguments
            .Select(
                expr =>
                    expr.NodeType == ExpressionType.Convert
                    && expr.Type == typeof(object)
                    && expr is UnaryExpression unaryExpr
                        ? unaryExpr.Operand
                        : expr
            )
            .Select(
                child =>
                    child.NodeType == ExpressionType.Conditional
                        ? StringifiedExpression.WithChildren(new[] { StringifiedExpression.TextOnly("("), this.ExpressionDispatch(child), StringifiedExpression.TextOnly(")"), })
                        : this.ExpressionDispatch(child)
            ).ToArray();
        var useVerbatimSyntax = ObjectToCodeImpl.UseVerbatimSyntax(config, formatString)
                || StringifiedExpression.WithChildren(interpolationArgumentsStringified).ToString().Contains('\n') // no longer necessary after https://devblogs.microsoft.com/dotnet/early-peek-at-csharp-11-features/#c-11-preview-allow-newlines-in-the-holes-of-interpolated-strings
            ;

        var parsed = FormatStringParser.ParseFormatString(formatString, interpolationArgumentsStringified.Cast<object>().ToArray());

        if (useVerbatimSyntax) {
            kids.Add("$@\"");
            foreach (var segment in parsed.segments) {
                kids.Add(segment.InitialStringPart.Replace("\"", "\"\"").Replace("{", "{{").Replace("}", "}}") + "{");
                kids.Add(segment.FollowedByValue is StringifiedExpression expr ? expr : throw new InvalidOperationException("All arguments should have been StringifiedExpressions"));
                if (segment.WithFormatString != null) {
                    kids.Add(":" + segment.WithFormatString + "}");
                } else {
                    kids.Add("}");
                }
            }

            kids.Add(parsed.Tail.Replace("\"", "\"\"").Replace("{", "{{").Replace("}", "}}") + "\"");
        } else {
            kids.Add("$\"");
            foreach (var segment in parsed.segments) {
                kids.Add(ObjectToCodeImpl.EscapeStringChars(segment.InitialStringPart.Replace("{", "{{").Replace("}", "}}")) + "{");
                kids.Add(segment.FollowedByValue is StringifiedExpression expr ? expr : throw new InvalidOperationException("All arguments should have been StringifiedExpressions"));
                if (segment.WithFormatString != null) {
                    kids.Add(":" + segment.WithFormatString + "}");
                } else {
                    kids.Add("}");
                }
            }

            kids.Add(ObjectToCodeImpl.EscapeStringChars(parsed.Tail.Replace("{", "{{").Replace("}", "}}")) + "\"");
        }

        return kids;
    }

    IEnumerable<StringifiedExpression> GetArgumentsForMethod(MethodBase methodInfo, IEnumerable<Expression> argValueExprs)
        => GetArgumentsForMethod(methodInfo.GetParameters(), argValueExprs);

    IEnumerable<StringifiedExpression> GetArgumentsForMethod(ParameterInfo[] parameters, IEnumerable<Expression> argValueExprs)
    {
        var argPrefixes = parameters.Select(p => p.IsOut ? "out " : p.ParameterType.IsByRef ? "ref " : null).ToArray();
        return argValueExprs.Zip(argPrefixes, (expr, prefix) => SingleChildDispatch(prefix, expr));
    }

    [Pure]
    StringifiedExpression StringifyMethodName(MethodCallExpression mce, MethodInfo method, Expression? objExpr)
    {
        var kids = KidsBuilder.Create();

        if (objExpr != null) {
            if (!(IsThisRef(objExpr) || IsClosureRef(objExpr))) {
                kids.Add(NestExpression(mce.NodeType, objExpr));
                kids.Add(".");
            }
        } else if (method.IsStatic && method.DeclaringType is not null) {
            kids.Add(TypeNameToCode(method.DeclaringType) + "."); //TODO:better reference avoiding for this?
        }

        kids.Add(method.Name + CreateGenericArgumentsIfNecessary(method), mce);
        return kids.Finish();
    }

    string CreateGenericArgumentsIfNecessary(MethodInfo method)
    {
        if (!method.IsGenericMethod) {
            return "";
        }

        if (!AlwaysUseExplicitTypeArguments && method.DeclaringType != null) {
            var genericMethodDefinition = method.GetGenericMethodDefinition();
            var relevantBindingFlagsForOverloads =
                BindingFlags.Public
                | (!method.IsPublic ? BindingFlags.NonPublic : 0)
                | (method.IsStatic ? BindingFlags.Static : BindingFlags.Instance);

            var confusibleOverloads = method.DeclaringType.GetTypeInfo()
                .GetMethods(relevantBindingFlagsForOverloads)
                .Where(
                    otherMethod =>
                        otherMethod != genericMethodDefinition
                        && otherMethod.Name == method.Name
                        && otherMethod.GetParameters().Select(pi => pi.ParameterType).SequenceEqual(method.GetParameters().Select(pi => pi.ParameterType))
                );

            if (!confusibleOverloads.Any()
                && genericMethodDefinition.GetGenericArguments()
                    .All(typeParameter => genericMethodDefinition.GetParameters().Any(parameter => ContainsInferableType(parameter.ParameterType, typeParameter)))) {
                return "";
            }
        }

        var methodTypeArgs = method.GetGenericArguments().Select(type => TypeNameToCode(type)).ToArray();
        return string.Concat("<", string.Join(", ", methodTypeArgs), ">");
    }

    static bool ContainsInferableType(Type haystack, Type needle)
        => haystack == needle
            || (haystack.IsArray || haystack.IsByRef) && ContainsInferableType(haystack.GetElementType() ?? throw new InvalidOperationException("arrays have elements"), needle)
            || haystack.GetTypeInfo().IsGenericType
            && !(typeof(Delegate).IsAssignableFrom(haystack)
                    && (haystack.GetTypeInfo().GetMethod("Invoke") ?? throw new InvalidOperationException("delegates have Invoke")).GetParameters().Any(pi => pi.ParameterType == needle)
                )
            && haystack.GetTypeInfo().GetGenericArguments().Any(argType => ContainsInferableType(argType, needle));

    [Pure]
    public StringifiedExpression DispatchIndex(Expression e)
    {
        var kids = KidsBuilder.Create();

        var ie = (IndexExpression)e;
        kids.Add(NestExpression(ie.NodeType, ie.Object ?? throw new("Assumption: indexer expressions have a receiver")));

        var args = ie.Indexer == null
            ? ie.Arguments.Select(SingleChildDispatch)
            : GetArgumentsForMethod(ie.Indexer.GetIndexParameters(), ie.Arguments);

        kids.Add(ArgListDispatch(args, ie, "[", "]"));
        return kids.Finish();
    }

    [Pure]
    public StringifiedExpression DispatchInvoke(Expression e)
    {
        var kids = KidsBuilder.Create();

        var ie = (InvocationExpression)e;
        if (ie.Expression.NodeType == ExpressionType.Lambda) {
            kids.Add("new " + TypeNameToCode(ie.Expression.Type));
        }

        kids.Add(NestExpression(ie.NodeType, ie.Expression));
        var invokeMethod = ie.Expression.Type.GetTypeInfo().GetMethod("Invoke") ?? throw new("Assumption: all delegates have a method Invoke");
        var args = GetArgumentsForMethod(invokeMethod, ie.Arguments);
        kids.Add(ArgListDispatch(args, ie));
        return kids.Finish();
    }

    [Pure]
    public StringifiedExpression DispatchConstant(Expression e)
    {
        var kids = KidsBuilder.Create();

        var const_Val = ((ConstantExpression)e).Value;
        var codeRepresentation = PlainObjectToCode(const_Val, e.Type);
        //e.Type.IsVisible
        if (codeRepresentation == null) {
            var typeClass = e.Type.GuessTypeClass();
            if (typeClass == ReflectionHelpers.TypeClass.NormalType) // probably this!
            {
                kids.Add("this"); //TODO:verify that all this references refer to the same object!
            } else {
                throw new ArgumentOutOfRangeException(
                    nameof(e),
                    "Can't print constant " + (const_Val?.ToString() ?? "<null>")
                    + " in expr of type " + e.Type
                );
            }
        } else {
            kids.Add(codeRepresentation);
        }

        return kids.Finish();
    }

    [Pure]
    public StringifiedExpression DispatchConditional(Expression e)
    {
        var kids = KidsBuilder.Create();

        var ce = (ConditionalExpression)e;
        kids.Add(NestExpression(ce.NodeType, ce.Test));
        kids.Add(" ? ", e);
        kids.Add(NestExpression(ce.NodeType, ce.IfTrue));
        kids.Add(" : ");
        kids.Add(NestExpression(ce.NodeType, ce.IfFalse));
        return kids.Finish();
    }

    [Pure]
    public StringifiedExpression DispatchListInit(Expression e)
    {
        var kids = KidsBuilder.Create();

        var lie = (ListInitExpression)e;
        kids.Add("new ", lie);
        kids.Add(TypeNameToCode(lie.NewExpression.Type));
        if (lie.NewExpression.Arguments.Any()) {
            kids.Add(ArgListDispatch(GetArgumentsForMethod(lie.NewExpression.Constructor ?? throw new("Assumption: Constructor cannot be omitted when it has arguments: " + lie.NewExpression.Type), lie.NewExpression.Arguments)));
        }

        kids.Add(" { ");
        kids.Add(JoinDispatch(lie.Initializers.Select(DispatchElementInit), ", "));
        kids.Add(" }");
        return kids.Finish();
    }

    [Pure]
    StringifiedExpression DispatchElementInit(ElementInit elemInit)
    {
        if (elemInit.Arguments.Count != 1) {
            return StringifiedExpression.WithChildren(ArgListDispatch(elemInit.Arguments.Select(SingleChildDispatch), null, "{ ", " }").ToArray()); //??
        } else {
            return SingleChildDispatch(elemInit.Arguments.Single());
        }
    }

    [Pure]
    StringifiedExpression DispatchMemberBinding(MemberBinding mb)
    {
        var kids = KidsBuilder.Create();

        kids.Add(mb.Member.Name + " = ");
        if (mb is MemberMemberBinding mmb) {
            kids.Add("{ ");
            kids.Add(JoinDispatch(mmb.Bindings.Select(DispatchMemberBinding), ", "));
            kids.Add(" }");
        } else if (mb is MemberListBinding mlb) {
            kids.Add("{ ");
            kids.Add(JoinDispatch(mlb.Initializers.Select(DispatchElementInit), ", "));
            kids.Add(" }");
        } else if (mb is MemberAssignment assignment) {
            kids.Add(SingleChildDispatch(assignment.Expression));
        } else {
            throw new NotImplementedException("Member binding of unknown type: " + mb.GetType());
        }

        return kids.Finish();
    }

    [Pure]
    public StringifiedExpression DispatchMemberInit(Expression e)
    {
        var kids = KidsBuilder.Create();

        var mie = (MemberInitExpression)e;
        kids.Add("new ", mie);
        var newExpr = mie.NewExpression;
        kids.Add(TypeNameToCode(newExpr.Type));
        if (newExpr.Arguments.Any()) {
            kids.Add(ArgListDispatch(GetArgumentsForMethod(newExpr.Constructor ?? throw new("Assumption: Constructor cannot be omitted when it has arguments: " + newExpr.Type), newExpr.Arguments)));
        }

        kids.Add(" { ");
        kids.Add(JoinDispatch(mie.Bindings.Select(DispatchMemberBinding), ", "));
        kids.Add(" }");
        return kids.Finish();
    }

    [Pure]
    public StringifiedExpression DispatchNew(Expression e)
    {
        var kids = KidsBuilder.Create();

        var ne = (NewExpression)e;
        if (ne.Type.GuessTypeClass() == ReflectionHelpers.TypeClass.AnonymousType) {
            var parameters = ne.Type.GetTypeInfo().GetConstructors().Single().GetParameters();
            var props = ne.Type.GetTypeInfo().GetProperties();
            if (
                !parameters.Select(p => (p.Name ?? "", p.ParameterType))
                    .SequenceEqual(props.Select(p => (p.Name, p.PropertyType)))) {
                throw new InvalidOperationException(
                    "Constructor params for anonymous type don't match it's properties!"
                );
            }

            if (!parameters.Select(p => p.ParameterType).SequenceEqual(ne.Arguments.Select(argE => argE.Type))) {
                throw new InvalidOperationException(
                    "Constructor Arguments for anonymous type don't match it's type signature!"
                );
            }

            kids.Add("new { ");
            for (var i = 0; i < props.Length; i++) {
                kids.Add(props[i].Name + " = ");
                kids.Add(SingleChildDispatch(ne.Arguments[i]));
                if (i + 1 < props.Length) {
                    kids.Add(", ");
                }
            }

            kids.Add(" }");
        } else {
            kids.Add("new " + TypeNameToCode(ne.Type), ne);
            if (ne.Arguments.Count == 0) {
                kids.Add("()");
            } else {
                kids.Add(ArgListDispatch(GetArgumentsForMethod(ne.Constructor ?? throw new("Assumption: any new-expression with arguments must have a constructor"), ne.Arguments)));
            }
        }

        //TODO: deal with anonymous types.
        return kids.Finish();
    }

    [Pure]
    public StringifiedExpression DispatchNewArrayInit(Expression e)
    {
        var kids = KidsBuilder.Create();

        var nae = (NewArrayExpression)e;
        var arrayElemType = nae.Type.GetElementType() ?? throw new("Assumption: all arrays have an element type");
        var isDelegate = typeof(Delegate).GetTypeInfo().IsAssignableFrom(arrayElemType);
        var implicitTypeOK = !isDelegate && nae.Expressions.Any()
            && nae.Expressions.All(expr => expr.Type == arrayElemType);
        kids.Add("new" + (implicitTypeOK ? "" : " " + TypeNameToCode(arrayElemType)) + "[] ", nae);
        kids.Add(ArgListDispatch(nae.Expressions.Select(SingleChildDispatch), null, "{ ", " }"));
        return kids.Finish();
    }

    [Pure]
    public StringifiedExpression DispatchNewArrayBounds(Expression e)
    {
        var kids = KidsBuilder.Create();

        var nae = (NewArrayExpression)e;
        var arrayElemType = nae.Type.GetElementType() ?? throw new("Assumption: all arrays have an element type");
        kids.Add("new " + TypeNameToCode(arrayElemType), nae);
        kids.Add(ArgListDispatch(nae.Expressions.Select(SingleChildDispatch), null, "[", "]"));
        return kids.Finish();
    }

    [Pure]
    public StringifiedExpression DispatchBlock(Expression e)
    {
        var kids = KidsBuilder.Create();

        var be = (BlockExpression)e;
        var hasReturn = be.Type != typeof(void);
        var statements = hasReturn ? be.Expressions.Take(be.Expressions.Count - 1) : be.Expressions;

        kids.Add("{ ");

        foreach (var v in be.Variables) {
            kids.Add(StatementDispatch(TypeNameToCode(v.Type), v, ExpressionType.Block));
        }

        foreach (var child in statements) {
            kids.Add(StatementDispatch(child, ExpressionType.Block));
        }

        if (hasReturn) {
            kids.Add(StatementDispatch("return", be.Result, ExpressionType.Block));
        }

        kids.Add("}");
        return kids.Finish();
    }
    #endregion

    #region Easy Cases
    [Pure]
    public StringifiedExpression DispatchPower(Expression e)
    {
        var kids = KidsBuilder.Create();

        kids.Add("Math.Pow", e);
        var binaryExpression = (BinaryExpression)e;
        kids.Add(ArgListDispatch(new[] { binaryExpression.Left, binaryExpression.Right, }.Select(SingleChildDispatch)));
        return kids.Finish();
    }

    [Pure]
    public StringifiedExpression DispatchAdd(Expression e)
        => BinaryDispatch("+", e);

    [Pure]
    public StringifiedExpression DispatchAddChecked(Expression e)
        => BinaryDispatch("+", e); //TODO: checked

    [Pure]
    public StringifiedExpression DispatchAnd(Expression e)
        => BinaryDispatch("&", e);

    [Pure]
    public StringifiedExpression DispatchAndAlso(Expression e)
        => BinaryDispatch("&&", e);

    [Pure]
    public StringifiedExpression DispatchArrayLength(Expression e)
    {
        var kids = KidsBuilder.Create();

        kids.Add(NestExpression(e.NodeType, ((UnaryExpression)e).Operand));
        kids.Add(".Length", e);
        return kids.Finish();
    }

    [Pure]
    public StringifiedExpression DispatchArrayIndex(Expression e)
    {
        var kids = KidsBuilder.Create();

        var binaryExpression = (BinaryExpression)e;
        kids.Add(NestExpression(e.NodeType, binaryExpression.Left));
        kids.Add("[", e);
        kids.Add(NestExpression(null, binaryExpression.Right));
        kids.Add("]");
        return kids.Finish();
    }

    [Pure]
    public StringifiedExpression DispatchCoalesce(Expression e)
        => ((BinaryExpression)e).Left is ConstantExpression ce && ((BinaryExpression)e).Right is ConstantExpression && ce.Type == e.Type && ce.Type == typeof(string) && ce.Value != null
            ? this.ExpressionDispatch(ce) //for some weird reason the compile sometimes generates redundant null-coalescing operators.  Get rid of em!
            : BinaryDispatch("??", e);

    [Pure]
    public StringifiedExpression DispatchConvert(Expression e)
        => UnaryDispatchConvert(e);

    [Pure]
    public StringifiedExpression DispatchConvertChecked(Expression e)
        => UnaryDispatchConvert(e);

    //TODO: get explicit and implicit conversion operators right.
    [Pure]
    public StringifiedExpression DispatchDivide(Expression e)
        => BinaryDispatch("/", e);

    [Pure]
    public StringifiedExpression DispatchEqual(Expression e)
        => BinaryDispatch("==", e);

    [Pure]
    public StringifiedExpression DispatchExclusiveOr(Expression e)
        => BinaryDispatch("^", e);

    [Pure]
    public StringifiedExpression DispatchGreaterThan(Expression e)
        => BinaryDispatch(">", e);

    [Pure]
    public StringifiedExpression DispatchGreaterThanOrEqual(Expression e)
        => BinaryDispatch(">=", e);

    [Pure]
    public StringifiedExpression DispatchLeftShift(Expression e)
        => BinaryDispatch("<<", e);

    [Pure]
    public StringifiedExpression DispatchLessThan(Expression e)
        => BinaryDispatch("<", e);

    [Pure]
    public StringifiedExpression DispatchLessThanOrEqual(Expression e)
        => BinaryDispatch("<=", e);

    [Pure]
    public StringifiedExpression DispatchModulo(Expression e)
        => BinaryDispatch("%", e);

    [Pure]
    public StringifiedExpression DispatchMultiply(Expression e)
        => BinaryDispatch("*", e);

    [Pure]
    public StringifiedExpression DispatchMultiplyChecked(Expression e)
        => BinaryDispatch("*", e);

    [Pure]
    public StringifiedExpression DispatchNegate(Expression e)
        => UnaryDispatch("-", e);

    [Pure]
    public StringifiedExpression DispatchUnaryPlus(Expression e)
        => UnaryDispatch("+", e);

    [Pure]
    public StringifiedExpression DispatchNegateChecked(Expression e)
        => UnaryDispatch("-", e);

    [Pure]
    public StringifiedExpression DispatchNot(Expression e)
        => UnaryDispatch(e.Type == typeof(bool) || e.Type == typeof(bool?) ? "!" : "~", e);

    [Pure]
    public StringifiedExpression DispatchNotEqual(Expression e)
        => BinaryDispatch("!=", e);

    [Pure]
    public StringifiedExpression DispatchOr(Expression e)
        => BinaryDispatch("|", e);

    [Pure]
    public StringifiedExpression DispatchOrElse(Expression e)
        => BinaryDispatch("||", e);

    [Pure]
    public StringifiedExpression DispatchParameter(Expression e)
    {
        var parameterExpression = (ParameterExpression)e;
        // ReSharper disable once ConstantNullCoalescingCondition
        return StringifiedExpression.TextAndExpr(parameterExpression.Name ?? parameterExpression.Type.Name + parameterExpression.GetHashCode(), e);
    }

    [Pure]
    public StringifiedExpression DispatchQuote(Expression e)
        => StringifiedExpression.WithChildren(NestExpression(e.NodeType, ((UnaryExpression)e).Operand).ToArray());

    [Pure]
    public StringifiedExpression DispatchRightShift(Expression e)
        => BinaryDispatch(">>", e);

    [Pure]
    public StringifiedExpression DispatchSubtract(Expression e)
        => BinaryDispatch("-", e);

    [Pure]
    public StringifiedExpression DispatchSubtractChecked(Expression e)
        => BinaryDispatch("-", e);

    [Pure]
    public StringifiedExpression DispatchTypeAs(Expression e)
        => UnaryPostfixDispatch(" as " + TypeNameToCode(e.Type), e);

    [Pure]
    public StringifiedExpression DispatchTypeIs(Expression e)
        => TypeOpDispatch("is", e);

    [Pure]
    public StringifiedExpression DispatchAssign(Expression e)
        => BinaryDispatch("=", e);

    [Pure]
    public StringifiedExpression DispatchDecrement(Expression e)
        => UnaryPostfixDispatch(" - 1", e);

    [Pure]
    public StringifiedExpression DispatchIncrement(Expression e)
        => UnaryPostfixDispatch(" + 1", e);

    [Pure]
    public StringifiedExpression DispatchAddAssign(Expression e)
        => BinaryDispatch("+=", e);

    [Pure]
    public StringifiedExpression DispatchAndAssign(Expression e)
        => BinaryDispatch("&=", e);

    [Pure]
    public StringifiedExpression DispatchDivideAssign(Expression e)
        => BinaryDispatch("/=", e);

    [Pure]
    public StringifiedExpression DispatchExclusiveOrAssign(Expression e)
        => BinaryDispatch("^=", e);

    [Pure]
    public StringifiedExpression DispatchLeftShiftAssign(Expression e)
        => BinaryDispatch("<<=", e);

    [Pure]
    public StringifiedExpression DispatchModuloAssign(Expression e)
        => BinaryDispatch("%=", e);

    [Pure]
    public StringifiedExpression DispatchMultiplyAssign(Expression e)
        => BinaryDispatch("*=", e);

    [Pure]
    public StringifiedExpression DispatchOrAssign(Expression e)
        => BinaryDispatch("|=", e);

    [Pure]
    public StringifiedExpression DispatchRightShiftAssign(Expression e)
        => BinaryDispatch(">>=", e);

    [Pure]
    public StringifiedExpression DispatchSubtractAssign(Expression e)
        => BinaryDispatch("-=", e);

    [Pure]
    public StringifiedExpression DispatchAddAssignChecked(Expression e)
        => BinaryDispatch("+=", e);

    [Pure]
    public StringifiedExpression DispatchMultiplyAssignChecked(Expression e)
        => BinaryDispatch("*=", e);

    [Pure]
    public StringifiedExpression DispatchSubtractAssignChecked(Expression e)
        => BinaryDispatch("-=", e);

    [Pure]
    public StringifiedExpression DispatchPreIncrementAssign(Expression e)
        => UnaryDispatch("++", e);

    [Pure]
    public StringifiedExpression DispatchPreDecrementAssign(Expression e)
        => UnaryDispatch("--", e);

    [Pure]
    public StringifiedExpression DispatchPostIncrementAssign(Expression e)
        => UnaryPostfixDispatch("++", e);

    [Pure]
    public StringifiedExpression DispatchPostDecrementAssign(Expression e)
        => UnaryPostfixDispatch("--", e);

    [Pure]
    public StringifiedExpression DispatchOnesComplement(Expression e)
        => UnaryDispatch("~", e);
    #endregion

    #region Unused by C#'s expression support; or unavailable in the language at all.
    [Pure]
    public StringifiedExpression DispatchTypeEqual(Expression e)
        => throw new NotImplementedException();

    [Pure]
    public StringifiedExpression DispatchDebugInfo(Expression e)
        => throw new NotImplementedException();

    [Pure]
    public StringifiedExpression DispatchDynamic(Expression e)
        => throw new NotImplementedException();

    [Pure]
    public StringifiedExpression DispatchDefault(Expression e)
    {
        var defExpr = (DefaultExpression)e;

        return StringifiedExpression.TextOnly("default(" + TypeNameToCode(defExpr.Type) + ")");
    }

    [Pure]
    public StringifiedExpression DispatchExtension(Expression e)
        => throw new NotImplementedException();

    [Pure]
    public StringifiedExpression DispatchGoto(Expression e)
        => throw new NotImplementedException();

    [Pure]
    public StringifiedExpression DispatchLabel(Expression e)
        => throw new NotImplementedException();

    [Pure]
    public StringifiedExpression DispatchRuntimeVariables(Expression e)
        => throw new NotImplementedException();

    [Pure]
    public StringifiedExpression DispatchLoop(Expression e)
        => throw new NotImplementedException();

    [Pure]
    public StringifiedExpression DispatchSwitch(Expression e)
        => throw new NotImplementedException();

    [Pure]
    public StringifiedExpression DispatchThrow(Expression e)
    {
        var kids = KidsBuilder.Create();
        var ue = (UnaryExpression)e;

        kids.Add("throw ");
        kids.Add(this.ExpressionDispatch(ue.Operand));

        return kids.Finish();
    }

    [Pure]
    public StringifiedExpression DispatchTry(Expression e)
        => throw new NotImplementedException();

    [Pure]
    public StringifiedExpression DispatchUnbox(Expression e)
        => throw new NotImplementedException();

    [Pure]
    public StringifiedExpression DispatchPowerAssign(Expression e)
        => throw new NotImplementedException();

    [Pure]
    public StringifiedExpression DispatchIsTrue(Expression e)
        => throw new NotImplementedException();

    [Pure]
    public StringifiedExpression DispatchIsFalse(Expression e)
        => throw new NotImplementedException();
    #endregion
}
