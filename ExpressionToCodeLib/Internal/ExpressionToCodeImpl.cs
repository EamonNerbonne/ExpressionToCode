using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ExpressionToCodeLib.Internal
{
    class ExpressionToCodeImpl : IExpressionTypeDispatch<StringifiedExpression>
    {
        #region General Helpers
        public ExpressionToCodeImpl(ExpressionToCodeConfiguration config) => this.config = config;
        readonly ExpressionToCodeConfiguration config;
        IObjectStringifier objectStringifier => config.Value.ObjectStringifier;
        bool alwaysUseExplicitTypeArguments => config.Value.AlwaysUseExplicitTypeArguments;

        [Pure]
        IEnumerable<StringifiedExpression> NestExpression(ExpressionType? parentType, Expression child, bool parensIfEqualRank = false)
        {
            var parentRank = parentType == null ? 0 : ExpressionPrecedence.Rank(parentType.Value);
            var needsParens = parentRank > 0
                && (parensIfEqualRank ? parentRank - 1 : parentRank) < ExpressionPrecedence.Rank(child.NodeType);

            if (needsParens) {
                yield return StringifiedExpression.TextOnly("(");
            }
            foreach (var grandchild in RawChildDispatch(child)) {
                yield return grandchild;
            }
            if (needsParens) {
                yield return StringifiedExpression.TextOnly(")");
            }
        }

        [Pure]
        IEnumerable<StringifiedExpression> RawChildDispatch(Expression child)
            => RawChildDispatch(new Argument { Expr = child });

        [Pure]
        IEnumerable<StringifiedExpression> RawChildDispatch(Argument child)
        {
            if (child.PrefixOrNull != null) {
                yield return StringifiedExpression.TextOnly(child.PrefixOrNull);
            }
            yield return this.ExpressionDispatch(child.Expr).MarkAsConceptualChild();
        }

        [Pure]
        static IEnumerable<StringifiedExpression> JoinDispatch<T>(IEnumerable<T> children, string joiner, Func<T, IEnumerable<StringifiedExpression>> childVisitor)
        {
            var isFirst = true;
            foreach (var child in children) {
                if (!isFirst) {
                    yield return StringifiedExpression.TextOnly(joiner);
                }
                foreach (var grandchild in childVisitor(child)) {
                    yield return grandchild;
                }
                isFirst = false;
            }
        }

        [Pure]
        IEnumerable<StringifiedExpression> JoinDispatch(IEnumerable<Argument> children, string joiner)
            => JoinDispatch(children, joiner, RawChildDispatch);

        struct Argument
        {
            public Expression Expr;
            public string PrefixOrNull;
        }

        [Pure]
        IEnumerable<StringifiedExpression> ArgListDispatch(
            IEnumerable<Argument> arguments,
            Expression value = null,
            string open = "(",
            string close = ")",
            string joiner = ", ")
        {
            if (value != null) {
                yield return StringifiedExpression.TextAndExpr(open, value);
            } else {
                yield return StringifiedExpression.TextOnly(open);
            }
            foreach (var o in JoinDispatch(arguments, joiner)) {
                yield return o;
            }
            yield return StringifiedExpression.TextOnly(close);
        }

        struct KidsBuilder
        {
            readonly List<StringifiedExpression> kids;

            KidsBuilder(List<StringifiedExpression> init)
                => kids = init;

            [Pure]
            public static KidsBuilder Create()
                => new KidsBuilder(new List<StringifiedExpression>());

            public void Add(StringifiedExpression node)
            {
                kids.Add(node);
            }

            public void Add(IEnumerable<StringifiedExpression> nodes)
            {
                kids.AddRange(nodes);
            }

            public void Add(string text, Expression value)
            {
                kids.Add(StringifiedExpression.TextAndExpr(text, value));
            }

            public void Add(string text)
            {
                kids.Add(StringifiedExpression.TextOnly(text));
            }

            public StringifiedExpression Finish()
                => StringifiedExpression.WithChildren(kids.ToArray());
        }

        [Pure]
        StringifiedExpression BinaryDispatch(string op, Expression e)
        {
            var kids = KidsBuilder.Create();
            var be = (BinaryExpression)e;
            Expression left,
                right;
            UnwrapEnumOp(be, out left, out right);
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
            var uleft = left.NodeType == ExpressionType.Convert ? ((UnaryExpression)left).Operand : null;
            var uright = right.NodeType == ExpressionType.Convert ? ((UnaryExpression)right).Operand : null;
            if (uleft != null) {
                if (uright != null) {
                    if (uright.Type.EnusureNullability() == uleft.Type.EnusureNullability()) {
                        left = uleft;
                        right = uright;
                    }
                } else {
                    UnwrapEnumBinOp(uleft, ref left, ref right);
                }
            } else if (uright != null) {
                //implies uleft == null
                UnwrapEnumBinOp(uright, ref right, ref left);
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

                if (expr2.NodeType == ExpressionType.Constant) {
                    var value = ((ConstantExpression)expr2).Value;
                    if (value == null) {
                        expr2 = Expression.Default(expr1uncast.Type.EnusureNullability());
                    } else if (expr1nonnullableType == typeof(char)) {
                        expr2 = Expression.Constant((char)(int)value);
                    } else {
                        expr2 = Expression.Constant(Enum.ToObject(expr1nonnullableType, value));
                    }
                } else {
                    expr2 = Expression.Convert(expr2, expr1uncast.Type);
                }
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
            if (!config.Value.OmitImplicitCasts || !ReflectionHelpers.CanImplicitlyCast(ue.Operand.Type, e.Type)) {
                if (e.Type.GetTypeInfo().IsAssignableFrom(ue.Operand.Type)) // base class, basically; don't re-print identical values.
                {
                    kids.Add("(" + objectStringifier.TypeNameToCode(e.Type) + ")");
                } else {
                    kids.Add("(" + objectStringifier.TypeNameToCode(e.Type) + ")", e);
                }
            }
            kids.Add(NestExpression(ue.NodeType, ue.Operand));
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
            kids.Add(objectStringifier.TypeNameToCode(((TypeBinaryExpression)e).TypeOperand));
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
                    : ArgListDispatch(le.Parameters.Select(pe => new Argument { Expr = pe }))
                //though delegate lambdas do support ref/out parameters, expression tree lambda's don't
                );
            kids.Add(" => ");
            kids.Add(NestExpression(le.NodeType, le.Body));
            return kids.Finish();
        }

        static bool isThisRef(Expression e)
            => e.NodeType == ExpressionType.Constant
                && ((ConstantExpression)e).Value != null
                && e.Type.GuessTypeClass() == ReflectionHelpers.TypeClass.NormalType;

        static bool isClosureRef(Expression e)
            => (
                e.NodeType == ExpressionType.Constant && ((ConstantExpression)e).Value != null
                    || e.NodeType == ExpressionType.MemberAccess
                )
                && e.Type.GuessTypeClass() == ReflectionHelpers.TypeClass.ClosureType;

        [Pure]
        public StringifiedExpression DispatchMemberAccess(Expression e)
        {
            var kids = KidsBuilder.Create();
            var me = (MemberExpression)e;
            var memberOfExpr = me.Expression;
            if (memberOfExpr != null && !isThisRef(memberOfExpr) && !isClosureRef(memberOfExpr)) {
                kids.Add(NestExpression(e.NodeType, memberOfExpr));
                kids.Add(".");
            } else if (ReflectionHelpers.IsMemberInfoStatic(me.Member)) {
                kids.Add(objectStringifier.TypeNameToCode(me.Member.DeclaringType) + ".");
            }

            kids.Add(me.Member.Name, e);
            return kids.Finish();
        }

        static readonly MethodInfo createDelegate = typeof(Delegate).GetTypeInfo()
            .GetMethod(
                "CreateDelegate",
                new[] { typeof(Type), typeof(object), typeof(MethodInfo) });

        [Pure]
        public StringifiedExpression DispatchCall(Expression e)
        {
            var kids = KidsBuilder.Create();

            var mce = (MethodCallExpression)e;

            var optPropertyInfo = ReflectionHelpers.GetPropertyIfGetter(mce.Method);
            if (optPropertyInfo != null
                && (optPropertyInfo.Name == "Item"
                    || mce.Object?.Type == typeof(string) && optPropertyInfo.Name == "Chars")) {
                kids.Add(NestExpression(mce.NodeType, mce.Object));
                //indexers don't support ref/out; so we can use unprefixed arguments
                kids.Add(ArgListDispatch(GetArgumentsForMethod(mce.Method, mce.Arguments), mce, "[", "]"));
            } else if (mce.Method.Equals(createDelegate) && mce.Arguments.Count == 3
                && mce.Arguments[2].NodeType == ExpressionType.Constant && mce.Arguments[2].Type == typeof(MethodInfo)) {
                //.net 4.0
                //implicitly constructed delegate from method group.
                var targetMethod = (MethodInfo)((ConstantExpression)mce.Arguments[2]).Value;
                var targetExpr = mce.Arguments[1].NodeType == ExpressionType.Constant
                    && ((ConstantExpression)mce.Arguments[1]).Value == null
                    ? null
                    : mce.Arguments[1];
                kids.Add(SinkMethodName(mce, targetMethod, targetExpr));
            } else if (mce.Method.Name == "CreateDelegate"
                && mce.Arguments.Count == 2
                && mce.Object?.Type == typeof(MethodInfo)
                && mce.Object.NodeType == ExpressionType.Constant
                && mce.Method.GetParameters()[1].ParameterType == typeof(object)
                ) {
                //.net 4.5
                //implicitly constructed delegate from method group.
                var targetMethod = (MethodInfo)((ConstantExpression)mce.Object).Value;
                var targetExpr = mce.Arguments[1].NodeType == ExpressionType.Constant
                    && ((ConstantExpression)mce.Arguments[1]).Value == null
                    ? null
                    : mce.Arguments[1];
                kids.Add(SinkMethodName(mce, targetMethod, targetExpr));
            } else if (mce.Object == null
                && mce.Type.FullName == "System.FormattableString"
                && mce.Method.DeclaringType?.FullName == "System.Runtime.CompilerServices.FormattableStringFactory"
                && mce.Method.Name == "Create"
                && mce.Arguments.Count == 2
                && mce.Arguments[0].NodeType == ExpressionType.Constant
                && mce.Arguments[1].NodeType == ExpressionType.NewArrayInit
                && ((NewArrayExpression)mce.Arguments[1]).Expressions.Count == 0
                ) {
                //.net 4.6
                //string-interpolations are compiled into FormattableStringFactory.Create
                var codeRepresentation = "$" + objectStringifier.PlainObjectToCode(((ConstantExpression)mce.Arguments[0]).Value, typeof(string));
                kids.Add(codeRepresentation);
            } else {
                var isExtensionMethod = mce.Method.IsStatic
                    && mce.Method.GetCustomAttributes(typeof(ExtensionAttribute), false).Any() && mce.Arguments.Any()
                    && mce.Object == null;
                var objectExpr = isExtensionMethod ? mce.Arguments.First() : mce.Object;
                kids.Add(SinkMethodName(mce, mce.Method, objectExpr));
                var args = GetArgumentsForMethod(mce.Method, mce.Arguments);

                kids.Add(ArgListDispatch(isExtensionMethod ? args.Skip(1) : args));
            }
            return kids.Finish();
        }

        static IEnumerable<Argument> GetArgumentsForMethod(MethodBase methodInfo, IEnumerable<Expression> argValueExprs)
            => GetArgumentsForMethod(methodInfo.GetParameters(), argValueExprs);

        static IEnumerable<Argument> GetArgumentsForMethod(ParameterInfo[] parameters, IEnumerable<Expression> argValueExprs)
        {
            var argPrefixes = parameters.Select(p => p.IsOut ? "out " : p.ParameterType.IsByRef ? "ref " : null).ToArray();
            return argValueExprs.Zip(argPrefixes, (expr, prefix) => new Argument { Expr = expr, PrefixOrNull = prefix });
        }

        [Pure]
        StringifiedExpression SinkMethodName(MethodCallExpression mce, MethodInfo method, Expression objExpr)
        {
            var kids = KidsBuilder.Create();

            if (objExpr != null) {
                if (!(isThisRef(objExpr) || isClosureRef(objExpr))) {
                    kids.Add(NestExpression(mce.NodeType, objExpr));
                    kids.Add(".");
                }
            } else if (method.IsStatic) {
                kids.Add(objectStringifier.TypeNameToCode(method.DeclaringType) + "."); //TODO:better reference avoiding for this?
            }
            var methodName = method.Name;

            methodName += CreateGenericArgumentsIfNecessary(method);
            kids.Add(methodName, mce);
            return kids.Finish();
        }

        string CreateGenericArgumentsIfNecessary(MethodInfo method)
        {
            if (!method.IsGenericMethod) {
                return "";
            }

            if (!alwaysUseExplicitTypeArguments) {
                var genericMethodDefinition = method.GetGenericMethodDefinition();
                var relevantBindingFlagsForOverloads =
                    BindingFlags.Public
                        | (!method.IsPublic ? BindingFlags.NonPublic : 0)
                        | (method.IsStatic ? BindingFlags.Static : BindingFlags.Instance)
                    ;

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

            var methodTypeArgs = method.GetGenericArguments().Select(type => objectStringifier.TypeNameToCode(type)).ToArray();
            return string.Concat("<", string.Join(", ", methodTypeArgs), ">");
        }

        static bool ContainsInferableType(Type haystack, Type needle)
            => haystack == needle
                || (haystack.IsArray || haystack.IsByRef) && ContainsInferableType(haystack.GetElementType(), needle)
                || haystack.GetTypeInfo().IsGenericType && haystack.GetTypeInfo().GetGenericArguments().Any(argType => ContainsInferableType(argType, needle));

        [Pure]
        public StringifiedExpression DispatchIndex(Expression e)
        {
            var kids = KidsBuilder.Create();

            var ie = (IndexExpression)e;
            kids.Add(NestExpression(ie.NodeType, ie.Object));

            var args = ie.Indexer == null
                ? ie.Arguments.Select(
                    a => new Argument {
                        Expr = a,
                        PrefixOrNull = null
                    })
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
                kids.Add("new " + objectStringifier.TypeNameToCode(ie.Expression.Type));
            }
            kids.Add(NestExpression(ie.NodeType, ie.Expression));
            var invokeMethod = ie.Expression.Type.GetTypeInfo().GetMethod("Invoke");
            var args = GetArgumentsForMethod(invokeMethod, ie.Arguments);
            kids.Add(ArgListDispatch(args, ie));
            return kids.Finish();
        }

        [Pure]
        public StringifiedExpression DispatchConstant(Expression e)
        {
            var kids = KidsBuilder.Create();

            var const_Val = ((ConstantExpression)e).Value;
            var codeRepresentation = objectStringifier.PlainObjectToCode(const_Val, e.Type);
            //e.Type.IsVisible
            if (codeRepresentation == null) {
                var typeclass = e.Type.GuessTypeClass();
                if (typeclass == ReflectionHelpers.TypeClass.NormalType) // probably this!
                {
                    kids.Add("this"); //TODO:verify that all this references refer to the same object!
                } else {
                    throw new ArgumentOutOfRangeException(
                        nameof(e),
                        "Can't print constant " + (const_Val?.ToString() ?? "<null>")
                            + " in expr of type " + e.Type);
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
            kids.Add(objectStringifier.TypeNameToCode(lie.NewExpression.Constructor.DeclaringType));
            if (lie.NewExpression.Arguments.Any()) {
                kids.Add(ArgListDispatch(GetArgumentsForMethod(lie.NewExpression.Constructor, lie.NewExpression.Arguments)));
            }

            kids.Add(" { ");
            kids.Add(JoinDispatch(lie.Initializers, ", ", DispatchElementInit));
            kids.Add(" }");
            return kids.Finish();
        }

        [Pure]
        IEnumerable<StringifiedExpression> DispatchElementInit(ElementInit elemInit)
        {
            if (elemInit.Arguments.Count != 1) {
                return ArgListDispatch(elemInit.Arguments.Select(ae => new Argument { Expr = ae }), null, "{ ", " }"); //??
            } else {
                return RawChildDispatch(elemInit.Arguments.Single());
            }
        }

        [Pure]
        IEnumerable<StringifiedExpression> DispatchMemberBinding(MemberBinding mb)
        {
            var kids = KidsBuilder.Create();

            kids.Add(mb.Member.Name + " = ");
            if (mb is MemberMemberBinding) {
                var mmb = (MemberMemberBinding)mb;
                kids.Add("{ ");
                kids.Add(JoinDispatch(mmb.Bindings, ", ", DispatchMemberBinding));
                kids.Add(" }");
            } else if (mb is MemberListBinding) {
                var mlb = (MemberListBinding)mb;
                kids.Add("{ ");
                kids.Add(JoinDispatch(mlb.Initializers, ", ", DispatchElementInit));
                kids.Add(" }");
            } else if (mb is MemberAssignment) {
                kids.Add(RawChildDispatch(((MemberAssignment)mb).Expression));
            } else {
                throw new NotImplementedException("Member binding of unknown type: " + mb.GetType());
            }
            return new[] { kids.Finish() };
        }

        [Pure]
        public StringifiedExpression DispatchMemberInit(Expression e)
        {
            var kids = KidsBuilder.Create();

            var mie = (MemberInitExpression)e;
            kids.Add("new ", mie);
            kids.Add(objectStringifier.TypeNameToCode(mie.NewExpression.Constructor.DeclaringType));
            if (mie.NewExpression.Arguments.Any()) {
                kids.Add(ArgListDispatch(GetArgumentsForMethod(mie.NewExpression.Constructor, mie.NewExpression.Arguments)));
            }

            kids.Add(" { ");
            kids.Add(JoinDispatch(mie.Bindings, ", ", DispatchMemberBinding));
            kids.Add(" }");
            return kids.Finish();
        }

        [Pure]
        public StringifiedExpression DispatchNew(Expression e)
        {
            var kids = KidsBuilder.Create();

            var ne = (NewExpression)e;
            if (ne.Type.GuessTypeClass() == ReflectionHelpers.TypeClass.AnonymousType) {
                var parms = ne.Type.GetTypeInfo().GetConstructors().Single().GetParameters();
                var props = ne.Type.GetTypeInfo().GetProperties();
                if (
                    !parms.Select(p => new { p.Name, Type = p.ParameterType })
                        .SequenceEqual(props.Select(p => new { p.Name, Type = p.PropertyType }))) {
                    throw new InvalidOperationException(
                        "Constructor params for anonymous type don't match it's properties!");
                }
                if (!parms.Select(p => p.ParameterType).SequenceEqual(ne.Arguments.Select(argE => argE.Type))) {
                    throw new InvalidOperationException(
                        "Constructor Arguments for anonymous type don't match it's type signature!");
                }
                kids.Add("new { ");
                for (var i = 0; i < props.Length; i++) {
                    kids.Add(props[i].Name + " = ");
                    kids.Add(RawChildDispatch(ne.Arguments[i]));
                    if (i + 1 < props.Length) {
                        kids.Add(", ");
                    }
                }
                kids.Add(" }");
            } else {
                kids.Add("new " + objectStringifier.TypeNameToCode(ne.Type), ne);
                kids.Add(ArgListDispatch(GetArgumentsForMethod(ne.Constructor, ne.Arguments)));
            }
            //TODO: deal with anonymous types.
            return kids.Finish();
        }

        [Pure]
        public StringifiedExpression DispatchNewArrayInit(Expression e)
        {
            var kids = KidsBuilder.Create();

            var nae = (NewArrayExpression)e;
            var arrayElemType = nae.Type.GetElementType();
            var isDelegate = typeof(Delegate).GetTypeInfo().IsAssignableFrom(arrayElemType);
            var implicitTypeOK = !isDelegate && nae.Expressions.Any()
                && nae.Expressions.All(expr => expr.Type == arrayElemType);
            kids.Add("new" + (implicitTypeOK ? "" : " " + objectStringifier.TypeNameToCode(arrayElemType)) + "[] ", nae);
            kids.Add(ArgListDispatch(nae.Expressions.Select(e1 => new Argument { Expr = e1 }), null, "{ ", " }"));
            return kids.Finish();
        }

        [Pure]
        public StringifiedExpression DispatchNewArrayBounds(Expression e)
        {
            var kids = KidsBuilder.Create();

            var nae = (NewArrayExpression)e;
            var arrayElemType = nae.Type.GetElementType();
            kids.Add("new " + objectStringifier.TypeNameToCode(arrayElemType), nae);
            kids.Add(ArgListDispatch(nae.Expressions.Select(e1 => new Argument { Expr = e1 }), null, "[", "]"));
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
                kids.Add(StatementDispatch(objectStringifier.TypeNameToCode(v.Type), v, ExpressionType.Block));
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
            kids.Add(ArgListDispatch(new[] { binaryExpression.Left, binaryExpression.Right }.Select(e1 => new Argument { Expr = e1 })));
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
            => BinaryDispatch("??", e);

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
            => UnaryPostfixDispatch(" as " + objectStringifier.TypeNameToCode(e.Type), e);

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

            return StringifiedExpression.TextOnly("default(" + objectStringifier.TypeNameToCode(defExpr.Type) + ")");
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
            => throw new NotImplementedException();

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
}
