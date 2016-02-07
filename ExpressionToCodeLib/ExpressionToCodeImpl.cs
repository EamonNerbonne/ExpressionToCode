using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Reflection;
using ExpressionToCodeLib.Unstable_v2_Api;

namespace ExpressionToCodeLib
{
    class ExpressionToCodeImpl : IExpressionTypeDispatch<StringifiedExpression>
    {
        #region General Helpers
        readonly IObjectToCode objectToCode;
        readonly bool explicitMethodTypeArgs;

        //TODO: refactor IExpressionTypeDispatch into an input/output model to avoid this tricky side-effect approach.
        internal ExpressionToCodeImpl(IObjectToCode objectToCode, bool explicitMethodTypeArgs)
        {
            this.objectToCode = objectToCode;
            this.explicitMethodTypeArgs = explicitMethodTypeArgs;
        }

        internal ExpressionToCodeImpl()
            : this(ObjectStringify.Default, false) { }

        [Pure]
        static StringifiedExpression Sink(string text) => StringifiedExpression.TextOnly(text);

        [Pure]
        static StringifiedExpression Sink(string text, Expression value) => StringifiedExpression.TextAndExpr(text, value);

        [Pure]
        IEnumerable<StringifiedExpression> NestExpression(ExpressionType? parentType, Expression child, bool parensIfEqualRank = false)
        {
            int parentRank = parentType == null ? 0 : ExpressionPrecedence.Rank(parentType.Value);
            bool needsParens = parentRank > 0
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
        IEnumerable<StringifiedExpression> RawChildDispatch(Expression child) => RawChildDispatch(new Argument { Expr = child });

        [Pure]
        IEnumerable<StringifiedExpression> RawChildDispatch(Argument child)
        {
            if (child.PrefixOrNull != null) {
                yield return Sink(child.PrefixOrNull);
            }
            yield return this.ExpressionDispatch(child.Expr);
        }

        [Pure]
        IEnumerable<StringifiedExpression> JoinDispatch<T>(IEnumerable<T> children, string joiner, Func<T, IEnumerable<StringifiedExpression>> childVisitor)
        {
            bool isFirst = true;
            foreach (var child in children) {
                if (!isFirst) {
                    yield return Sink(joiner);
                }
                foreach (var grandchild in childVisitor(child)) {
                    yield return grandchild;
                }
                isFirst = false;
            }
        }

        [Pure]
        IEnumerable<StringifiedExpression> JoinDispatch(IEnumerable<Argument> children, string joiner) => JoinDispatch(children, joiner, RawChildDispatch);

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
                yield return Sink(open, value);
            } else {
                yield return Sink(open);
            }
            foreach (var o in JoinDispatch(arguments, joiner)) {
                yield return o;
            }
            yield return Sink(close);
        }

        struct KidsBuilder
        {
            readonly List<StringifiedExpression> kids;
            KidsBuilder(List<StringifiedExpression> init) { kids = init; }

            public static KidsBuilder Create() => new KidsBuilder(new List<StringifiedExpression>());

            public void Add(StringifiedExpression node) { kids.Add(node); }
            public void Add(IEnumerable<StringifiedExpression> nodes) { kids.AddRange(nodes); }
            public void Add(string text, Expression value) { kids.Add(StringifiedExpression.TextAndExpr(text, value)); }
            public StringifiedExpression Finish() { return StringifiedExpression.WithChildren(kids.ToArray()); }
        }

        [Pure]
        StringifiedExpression BinaryDispatch(string op, Expression e)
        {
            var kids = KidsBuilder.Create();
            var be = (BinaryExpression)e;
            Expression left, right;
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
            } else
            //uleft != null
                if (uright != null) {
                    UnwrapEnumBinOp(uright, ref right, ref left);
                }
        }

        [Pure]
        static void UnwrapEnumBinOp(Expression expr1uncast, ref Expression expr1, ref Expression expr2)
        {
            Type expr1nonnullableType = expr1uncast.Type.AvoidNullability();
            Type expr2nonnullableType = expr2.Type.AvoidNullability();
            if (expr1nonnullableType.IsEnum
                && expr1nonnullableType.GetEnumUnderlyingType() == expr2nonnullableType
                || expr1nonnullableType == typeof(char) && expr2nonnullableType == typeof(int)
                ) {
                expr1 = expr1uncast;

                if (expr2.NodeType == ExpressionType.Constant) {
                    object value = ((ConstantExpression)expr2).Value;
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
            bool needsSpace = ExpressionPrecedence.TokenizerConfusable(ue.NodeType, ue.Operand.NodeType);
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
            if (e.Type.IsAssignableFrom(ue.Operand.Type)) // base class, basically; don't re-print identical values.
            {
                Sink("(" + objectToCode.TypeNameToCode(e.Type) + ")");
            } else {
                Sink("(" + objectToCode.TypeNameToCode(e.Type) + ")", e);
            }
            NestExpression(ue.NodeType, ue.Operand);
        }

        [Pure]
        StringifiedExpression UnaryPostfixDispatch(string op, Expression e)
        {
            var kids = KidsBuilder.Create();
            var ue = (UnaryExpression)e;
            NestExpression(ue.NodeType, ue.Operand);
            Sink(op, e);
        }

        [Pure]
        StringifiedExpression TypeOpDispatch(string op, Expression e)
        {
            var kids = KidsBuilder.Create();
            NestExpression(e.NodeType, ((TypeBinaryExpression)e).Expression);
            Sink(" " + op + " ", e);
            Sink(objectToCode.TypeNameToCode(((TypeBinaryExpression)e).TypeOperand));
        }

        [Pure]
        StringifiedExpression StatementDispatch(Expression e, ExpressionType? parentType = null)
        {
            var kids = KidsBuilder.Create();
            NestExpression(parentType, e);
            Sink("; ");
        }

        [Pure]
        StringifiedExpression StatementDispatch(String prefix, Expression e, ExpressionType? parentType = null)
        {
            var kids = KidsBuilder.Create();
            Sink(prefix);
            Sink(" ");
            StatementDispatch(e, parentType);
        }
        #endregion

        #region Hard Cases
        [Pure]
        public StringifiedExpression DispatchLambda(Expression e)
        {
            var kids = KidsBuilder.Create();
            var le = (LambdaExpression)e;
            if (le.Parameters.Count == 1) {
                NestExpression(e.NodeType, le.Parameters.Single());
            } else {
                //though delegate lambdas do support ref/out parameters, expression tree lambda's don't
                ArgListDispatch(le.Parameters.Select(pe => new Argument { Expr = pe }));
            }
            Sink(" => ");
            NestExpression(le.NodeType, le.Body);
        }

        static bool isThisRef(Expression e)
        {
            return
                e.NodeType == ExpressionType.Constant && ((ConstantExpression)e).Value != null
                    && e.Type.GuessTypeClass() == ReflectionHelpers.TypeClass.NormalType;
        }

        static bool isClosureRef(Expression e)
        {
            return
                e.NodeType == ExpressionType.Constant && ((ConstantExpression)e).Value != null
                    && e.Type.GuessTypeClass() == ReflectionHelpers.TypeClass.ClosureType;
        }

        [Pure]
        public StringifiedExpression DispatchMemberAccess(Expression e)
        {
            var kids = KidsBuilder.Create();
            var me = (MemberExpression)e;
            Expression memberOfExpr = me.Expression;
            if (memberOfExpr != null && !isThisRef(memberOfExpr) && !isClosureRef(memberOfExpr)) {
                NestExpression(e.NodeType, memberOfExpr);
                Sink(".");
            } else if (ReflectionHelpers.IsMemberInfoStatic(me.Member)) {
                Sink(objectToCode.TypeNameToCode(me.Member.ReflectedType) + ".");
            }

            Sink(me.Member.Name, e);
        }

        static readonly MethodInfo createDelegate = typeof(Delegate).GetMethod(
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
                    || mce.Object.Type == typeof(string) && optPropertyInfo.Name == "Chars")) {
                NestExpression(mce.NodeType, mce.Object);
                //indexers don't support ref/out; so we can use unprefixed arguments
                ArgListDispatch(GetArgumentsForMethod(mce.Method, mce.Arguments), mce, "[", "]");
            } else if (mce.Method.Equals(createDelegate) && mce.Arguments.Count == 3
                && mce.Arguments[2].NodeType == ExpressionType.Constant && mce.Arguments[2].Type == typeof(MethodInfo)) {
                //.net 4.0
                //implicitly constructed delegate from method group.
                var targetMethod = (MethodInfo)((ConstantExpression)mce.Arguments[2]).Value;
                var targetExpr = mce.Arguments[1].NodeType == ExpressionType.Constant
                    && ((ConstantExpression)mce.Arguments[1]).Value == null
                    ? null
                    : mce.Arguments[1];
                SinkMethodName(mce, targetMethod, targetExpr);
            } else if (mce.Method.Name == "CreateDelegate"
                && mce.Arguments.Count == 2
                && mce.Object.Type == typeof(MethodInfo)
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
                SinkMethodName(mce, targetMethod, targetExpr);
            } else if (mce.Object == null
                && mce.Type.FullName == "System.FormattableString"
                && mce.Method.DeclaringType.FullName == "System.Runtime.CompilerServices.FormattableStringFactory"
                && mce.Method.Name == "Create"
                && mce.Arguments.Count == 2
                && mce.Arguments[0].NodeType == ExpressionType.Constant
                && mce.Arguments[1].NodeType == ExpressionType.NewArrayInit
                && ((NewArrayExpression)mce.Arguments[1]).Expressions.Count == 0
                ) {
                //.net 4.6
                //string-interpolations are compiled into FormattableStringFactory.Create
                var codeRepresentation = "$" + objectToCode.PlainObjectToCode(((ConstantExpression)mce.Arguments[0]).Value, typeof(string));
                Sink(codeRepresentation);
            } else {
                bool isExtensionMethod = mce.Method.IsStatic
                    && mce.Method.GetCustomAttributes(typeof(ExtensionAttribute), false).Any() && mce.Arguments.Any()
                    && mce.Object == null;
                Expression objectExpr = isExtensionMethod ? mce.Arguments.First() : mce.Object;
                SinkMethodName(mce, mce.Method, objectExpr);
                var args = GetArgumentsForMethod(mce.Method, mce.Arguments);

                ArgListDispatch(isExtensionMethod ? args.Skip(1) : args);
            }
        }

        static IEnumerable<Argument> GetArgumentsForMethod(MethodBase methodInfo, IEnumerable<Expression> argValueExprs)
        {
            return GetArgumentsForMethod(methodInfo.GetParameters(), argValueExprs);
        }

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
                    NestExpression(mce.NodeType, objExpr);
                    Sink(".");
                }
            } else if (method.IsStatic) {
                Sink(objectToCode.TypeNameToCode(method.DeclaringType) + "."); //TODO:better reference avoiding for this?
            }
            var methodName = method.Name;

            methodName += CreateGenericArgumentsIfNecessary(mce, method);
            Sink(methodName, mce);
        }

        string CreateGenericArgumentsIfNecessary(MethodCallExpression mce, MethodInfo method)
        {
            if (!method.IsGenericMethod) {
                return "";
            }

            if (!explicitMethodTypeArgs) {
                var genericMethodDefinition = method.GetGenericMethodDefinition();
                var relevantBindingFlagsForOverloads =
                    BindingFlags.Public
                        | (!method.IsPublic ? BindingFlags.NonPublic : 0)
                        | (method.IsStatic ? BindingFlags.Static : BindingFlags.Instance)
                    ;

                var confusibleOverloads = method.DeclaringType.GetMethods(relevantBindingFlagsForOverloads)
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

            var methodTypeArgs = method.GetGenericArguments().Select(type => objectToCode.TypeNameToCode(type)).ToArray();
            return string.Concat("<", string.Join(", ", methodTypeArgs), ">");
        }

        static bool ContainsInferableType(Type haystack, Type needle)
        {
            return haystack == needle
                || (haystack.IsArray || haystack.IsByRef) && ContainsInferableType(haystack.GetElementType(), needle)
                || haystack.IsGenericType && haystack.GetGenericArguments().Any(argType => ContainsInferableType(argType, needle));
        }

        [Pure]
        public StringifiedExpression DispatchIndex(Expression e)
        {
            var kids = KidsBuilder.Create();

            var ie = (IndexExpression)e;
            NestExpression(ie.NodeType, ie.Object);

            var args = ie.Indexer == null
                ? ie.Arguments.Select(
                    a => new Argument {
                        Expr = a,
                        PrefixOrNull = null
                    })
                : // else {
                GetArgumentsForMethod(ie.Indexer.GetIndexParameters(), ie.Arguments);

            ArgListDispatch(args, ie, "[", "]");
        }

        [Pure]
        public StringifiedExpression DispatchInvoke(Expression e)
        {
            var kids = KidsBuilder.Create();

            var ie = (InvocationExpression)e;
            if (ie.Expression.NodeType == ExpressionType.Lambda) {
                Sink("new " + objectToCode.TypeNameToCode(ie.Expression.Type));
            }
            NestExpression(ie.NodeType, ie.Expression);
            var invokeMethod = ie.Expression.Type.GetMethod("Invoke");
            var args = GetArgumentsForMethod(invokeMethod, ie.Arguments);
            ArgListDispatch(args, ie);
        }

        [Pure]
        public StringifiedExpression DispatchConstant(Expression e)
        {
            var kids = KidsBuilder.Create();

            var const_Val = ((ConstantExpression)e).Value;
            string codeRepresentation = objectToCode.PlainObjectToCode(const_Val, e.Type);
            //e.Type.IsVisible
            if (codeRepresentation == null) {
                var typeclass = e.Type.GuessTypeClass();
                if (typeclass == ReflectionHelpers.TypeClass.NormalType) // probably this!
                {
                    Sink("this"); //TODO:verify that all this references refer to the same object!
                } else {
                    throw new ArgumentOutOfRangeException(
                        "e",
                        "Can't print constant " + (const_Val == null ? "<null>" : const_Val.ToString())
                            + " in expr of type " + e.Type);
                }
            } else {
                Sink(codeRepresentation);
            }
        }

        [Pure]
        public StringifiedExpression DispatchConditional(Expression e)
        {
            var kids = KidsBuilder.Create();

            var ce = (ConditionalExpression)e;
            NestExpression(ce.NodeType, ce.Test);
            Sink(" ? ", e);
            NestExpression(ce.NodeType, ce.IfTrue);
            Sink(" : ");
            NestExpression(ce.NodeType, ce.IfFalse);
        }

        [Pure]
        public StringifiedExpression DispatchListInit(Expression e)
        {
            var kids = KidsBuilder.Create();

            var lie = (ListInitExpression)e;
            Sink("new ", lie);
            Sink(objectToCode.TypeNameToCode(lie.NewExpression.Constructor.ReflectedType));
            if (lie.NewExpression.Arguments.Any()) {
                ArgListDispatch(GetArgumentsForMethod(lie.NewExpression.Constructor, lie.NewExpression.Arguments));
            }

            Sink(" { ");
            JoinDispatch(lie.Initializers, ", ", DispatchElementInit);
            Sink(" }");
        }

        [Pure]
        StringifiedExpression DispatchElementInit(ElementInit elemInit)
        {
            var kids = KidsBuilder.Create();

            if (elemInit.Arguments.Count != 1) {
                ArgListDispatch(elemInit.Arguments.Select(ae => new Argument { Expr = ae }), null, "{ ", " }"); //??
            } else {
                RawChildDispatch(elemInit.Arguments.Single());
            }
        }

        [Pure]
        StringifiedExpression DispatchMemberBinding(MemberBinding mb)
        {
            var kids = KidsBuilder.Create();

            Sink(mb.Member.Name + " = ");
            if (mb is MemberMemberBinding) {
                var mmb = (MemberMemberBinding)mb;
                Sink("{ ");
                JoinDispatch(mmb.Bindings, ", ", DispatchMemberBinding);
                Sink(" }");
            } else if (mb is MemberListBinding) {
                var mlb = (MemberListBinding)mb;
                Sink("{ ");
                JoinDispatch(mlb.Initializers, ", ", DispatchElementInit);
                Sink(" }");
            } else if (mb is MemberAssignment) {
                RawChildDispatch(((MemberAssignment)mb).Expression);
            } else {
                throw new NotImplementedException("Member binding of unknown type: " + mb.GetType());
            }
        }

        [Pure]
        public StringifiedExpression DispatchMemberInit(Expression e)
        {
            var kids = KidsBuilder.Create();

            var mie = (MemberInitExpression)e;
            Sink("new ", mie);
            Sink(objectToCode.TypeNameToCode(mie.NewExpression.Constructor.ReflectedType));
            if (mie.NewExpression.Arguments.Any()) {
                ArgListDispatch(GetArgumentsForMethod(mie.NewExpression.Constructor, mie.NewExpression.Arguments));
            }

            Sink(" { ");
            JoinDispatch(mie.Bindings, ", ", DispatchMemberBinding);
            Sink(" }");
        }

        [Pure]
        public StringifiedExpression DispatchNew(Expression e)
        {
            var kids = KidsBuilder.Create();

            var ne = (NewExpression)e;
            if (ne.Type.GuessTypeClass() == ReflectionHelpers.TypeClass.AnonymousType) {
                var parms = ne.Type.GetConstructors().Single().GetParameters();
                var props = ne.Type.GetProperties();
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
                Sink("new { ");
                for (int i = 0; i < props.Length; i++) {
                    Sink(props[i].Name + " = ");
                    RawChildDispatch(ne.Arguments[i]);
                    if (i + 1 < props.Length) {
                        Sink(", ");
                    }
                }
                Sink(" }");
            } else {
                Sink("new " + objectToCode.TypeNameToCode(ne.Type), ne);
                ArgListDispatch(GetArgumentsForMethod(ne.Constructor, ne.Arguments));
            }
            //TODO: deal with anonymous types.
        }

        [Pure]
        public StringifiedExpression DispatchNewArrayInit(Expression e)
        {
            var kids = KidsBuilder.Create();

            var nae = (NewArrayExpression)e;
            Type arrayElemType = nae.Type.GetElementType();
            bool isDelegate = typeof(Delegate).IsAssignableFrom(arrayElemType);
            bool implicitTypeOK = !isDelegate && nae.Expressions.Any()
                && nae.Expressions.All(expr => expr.Type == arrayElemType);
            Sink("new" + (implicitTypeOK ? "" : " " + objectToCode.TypeNameToCode(arrayElemType)) + "[] ", nae);
            ArgListDispatch(nae.Expressions.Select(e1 => new Argument { Expr = e1 }), null, "{ ", " }");
        }

        [Pure]
        public StringifiedExpression DispatchNewArrayBounds(Expression e)
        {
            var kids = KidsBuilder.Create();

            var nae = (NewArrayExpression)e;
            Type arrayElemType = nae.Type.GetElementType();
            Sink("new " + objectToCode.TypeNameToCode(arrayElemType), nae);
            ArgListDispatch(nae.Expressions.Select(e1 => new Argument { Expr = e1 }), null, "[", "]");
        }

        [Pure]
        public StringifiedExpression DispatchBlock(Expression e)
        {
            var kids = KidsBuilder.Create();

            var be = (BlockExpression)e;
            bool hasReturn = (be.Type != typeof(StringifiedExpression));
            var statements = hasReturn ? be.Expressions.Take(be.Expressions.Count - 1) : be.Expressions;

            Sink("{ ");

            foreach (var v in be.Variables) {
                StatementDispatch(objectToCode.TypeNameToCode(v.Type), v, ExpressionType.Block);
            }

            foreach (var child in statements) {
                StatementDispatch(child, ExpressionType.Block);
            }

            if (hasReturn) {
                StatementDispatch("return", be.Result, ExpressionType.Block);
            }

            Sink("}");
        }
        #endregion

        #region Easy Cases
        [Pure]
        public StringifiedExpression DispatchPower(Expression e)
        {
            var kids = KidsBuilder.Create();

            Sink("Math.Pow", e);
            var binaryExpression = (BinaryExpression)e;
            ArgListDispatch(new[] { binaryExpression.Left, binaryExpression.Right }.Select(e1 => new Argument { Expr = e1 }));
        }

        [Pure]
        public StringifiedExpression DispatchAdd(Expression e)
        {
            return BinaryDispatch("+", e);
        }

        [Pure]
        public StringifiedExpression DispatchAddChecked(Expression e)
        {
            return BinaryDispatch("+", e);
        } //TODO: checked

        [Pure]
        public StringifiedExpression DispatchAnd(Expression e)
        {
            return BinaryDispatch("&", e);
        }

        [Pure]
        public StringifiedExpression DispatchAndAlso(Expression e)
        {
            return BinaryDispatch("&&", e);
        }

        [Pure]
        public StringifiedExpression DispatchArrayLength(Expression e)
        {
            var kids = KidsBuilder.Create();

            NestExpression(e.NodeType, ((UnaryExpression)e).Operand);
            Sink(".Length", e);
        }

        [Pure]
        public StringifiedExpression DispatchArrayIndex(Expression e)
        {
            var kids = KidsBuilder.Create();

            var binaryExpression = (BinaryExpression)e;
            NestExpression(e.NodeType, binaryExpression.Left);
            Sink("[", e);
            NestExpression(null, binaryExpression.Right);
            Sink("]");
        }

        [Pure]
        public StringifiedExpression DispatchCoalesce(Expression e)
        {
            return BinaryDispatch("??", e);
        }

        [Pure]
        public StringifiedExpression DispatchConvert(Expression e)
        {
            return UnaryDispatchConvert(e);
        }

        [Pure]
        public StringifiedExpression DispatchConvertChecked(Expression e)
        {
            return UnaryDispatchConvert(e);
        }

        //TODO: get explicit and implicit conversion operators right.
        [Pure]
        public StringifiedExpression DispatchDivide(Expression e)
        {
            var kids = KidsBuilder.Create();

            BinaryDispatch("/", e);
        }

        [Pure]
        public StringifiedExpression DispatchEqual(Expression e)
        {
            return BinaryDispatch("==", e);
        }

        [Pure]
        public StringifiedExpression DispatchExclusiveOr(Expression e)
        {
            return BinaryDispatch("^", e);
        }

        [Pure]
        public StringifiedExpression DispatchGreaterThan(Expression e)
        {
            return BinaryDispatch(">", e);
        }

        [Pure]
        public StringifiedExpression DispatchGreaterThanOrEqual(Expression e)
        {
            return BinaryDispatch(">=", e);
        }

        [Pure]
        public StringifiedExpression DispatchLeftShift(Expression e)
        {
            return BinaryDispatch("<<", e);
        }

        [Pure]
        public StringifiedExpression DispatchLessThan(Expression e)
        {
            return BinaryDispatch("<", e);
        }

        [Pure]
        public StringifiedExpression DispatchLessThanOrEqual(Expression e)
        {
            return BinaryDispatch("<=", e);
        }

        [Pure]
        public StringifiedExpression DispatchModulo(Expression e)
        {
            return BinaryDispatch("%", e);
        }

        [Pure]
        public StringifiedExpression DispatchMultiply(Expression e)
        {
            return BinaryDispatch("*", e);
        }

        [Pure]
        public StringifiedExpression DispatchMultiplyChecked(Expression e)
        {
            return BinaryDispatch("*", e);
        }

        [Pure]
        public StringifiedExpression DispatchNegate(Expression e)
        {
            return UnaryDispatch("-", e);
        }

        [Pure]
        public StringifiedExpression DispatchUnaryPlus(Expression e)
        {
            return UnaryDispatch("+", e);
        }

        [Pure]
        public StringifiedExpression DispatchNegateChecked(Expression e)
        {
            return UnaryDispatch("-", e);
        }

        [Pure]
        public StringifiedExpression DispatchNot(Expression e)
        {
            return UnaryDispatch(e.Type == typeof(bool) || e.Type == typeof(bool?) ? "!" : "~", e);
        }

        [Pure]
        public StringifiedExpression DispatchNotEqual(Expression e)
        {
            return BinaryDispatch("!=", e);
        }

        [Pure]
        public StringifiedExpression DispatchOr(Expression e)
        {
            return BinaryDispatch("|", e);
        }

        [Pure]
        public StringifiedExpression DispatchOrElse(Expression e)
        {
            return BinaryDispatch("||", e);
        }

        [Pure]
        public StringifiedExpression DispatchParameter(Expression e)
        {
            var parameterExpression = ((ParameterExpression)e);
            return Sink(parameterExpression.Name ?? (parameterExpression.Type.Name + parameterExpression.GetHashCode()), e);
        }

        [Pure]
        public StringifiedExpression DispatchQuote(Expression e)
        {
            return KidsBuilder.Finish(NestExpression(e.NodeType, ((UnaryExpression)e).Operand));
        }

        [Pure]
        public StringifiedExpression DispatchRightShift(Expression e)
        {
            return BinaryDispatch(">>", e);
        }

        [Pure]
        public StringifiedExpression DispatchSubtract(Expression e)
        {
            return BinaryDispatch("-", e);
        }

        [Pure]
        public StringifiedExpression DispatchSubtractChecked(Expression e)
        {
            return BinaryDispatch("-", e);
        }

        [Pure]
        public StringifiedExpression DispatchTypeAs(Expression e)
        {
            return UnaryPostfixDispatch(" as " + objectToCode.TypeNameToCode(e.Type), e);
        }

        [Pure]
        public StringifiedExpression DispatchTypeIs(Expression e)
        {
            return TypeOpDispatch("is", e);
        }

        [Pure]
        public StringifiedExpression DispatchAssign(Expression e)
        {
            return BinaryDispatch("=", e);
        }

        [Pure]
        public StringifiedExpression DispatchDecrement(Expression e)
        {
            return UnaryPostfixDispatch(" - 1", e);
        }

        [Pure]
        public StringifiedExpression DispatchIncrement(Expression e)
        {
            return UnaryPostfixDispatch(" + 1", e);
        }

        [Pure]
        public StringifiedExpression DispatchAddAssign(Expression e)
        {
            return BinaryDispatch("+=", e);
        }

        [Pure]
        public StringifiedExpression DispatchAndAssign(Expression e)
        {
            return BinaryDispatch("&=", e);
        }

        [Pure]
        public StringifiedExpression DispatchDivideAssign(Expression e)
        {
            return BinaryDispatch("/=", e);
        }

        [Pure]
        public StringifiedExpression DispatchExclusiveOrAssign(Expression e)
        {
            return BinaryDispatch("^=", e);
        }

        [Pure]
        public StringifiedExpression DispatchLeftShiftAssign(Expression e)
        {
            return BinaryDispatch("<<=", e);
        }

        [Pure]
        public StringifiedExpression DispatchModuloAssign(Expression e)
        {
            return BinaryDispatch("%=", e);
        }

        [Pure]
        public StringifiedExpression DispatchMultiplyAssign(Expression e)
        {
            return BinaryDispatch("*=", e);
        }

        [Pure]
        public StringifiedExpression DispatchOrAssign(Expression e)
        {
            return BinaryDispatch("|=", e);
        }

        [Pure]
        public StringifiedExpression DispatchRightShiftAssign(Expression e)
        {
            return BinaryDispatch(">>=", e);
        }

        [Pure]
        public StringifiedExpression DispatchSubtractAssign(Expression e)
        {
            return BinaryDispatch("-=", e);
        }

        [Pure]
        public StringifiedExpression DispatchAddAssignChecked(Expression e)
        {
            return BinaryDispatch("+=", e);
        }

        [Pure]
        public StringifiedExpression DispatchMultiplyAssignChecked(Expression e)
        {
            return BinaryDispatch("*=", e);
        }

        [Pure]
        public StringifiedExpression DispatchSubtractAssignChecked(Expression e)
        {
            return BinaryDispatch("-=", e);
        }

        [Pure]
        public StringifiedExpression DispatchPreIncrementAssign(Expression e)
        {
            return UnaryDispatch("++", e);
        }

        [Pure]
        public StringifiedExpression DispatchPreDecrementAssign(Expression e)
        {
            return UnaryDispatch("--", e);
        }

        [Pure]
        public StringifiedExpression DispatchPostIncrementAssign(Expression e)
        {
            return UnaryPostfixDispatch("++", e);
        }

        [Pure]
        public StringifiedExpression DispatchPostDecrementAssign(Expression e)
        {
            return UnaryPostfixDispatch("--", e);
        }

        [Pure]
        public StringifiedExpression DispatchOnesComplement(Expression e)
        {
            return UnaryDispatch("~", e);
        }
        #endregion

        #region Unused by C#'s expression support; or unavailable in the language at all.
        [Pure]
        public StringifiedExpression DispatchTypeEqual(Expression e)
        {
            throw new NotImplementedException();
        }

        [Pure]
        public StringifiedExpression DispatchDebugInfo(Expression e)
        {
            throw new NotImplementedException();
        }

        [Pure]
        public StringifiedExpression DispatchDynamic(Expression e)
        {
            throw new NotImplementedException();
        }

        [Pure]
        public StringifiedExpression DispatchDefault(Expression e)
        {
            var defExpr = (DefaultExpression)e;

            return Sink("default(" + objectToCode.TypeNameToCode(defExpr.Type) + ")");
        }

        [Pure]
        public StringifiedExpression DispatchExtension(Expression e)
        {
            throw new NotImplementedException();
        }

        [Pure]
        public StringifiedExpression DispatchGoto(Expression e)
        {
            throw new NotImplementedException();
        }

        [Pure]
        public StringifiedExpression DispatchLabel(Expression e)
        {
            throw new NotImplementedException();
        }

        [Pure]
        public StringifiedExpression DispatchRuntimeVariables(Expression e)
        {
            throw new NotImplementedException();
        }

        [Pure]
        public StringifiedExpression DispatchLoop(Expression e)
        {
            throw new NotImplementedException();
        }

        [Pure]
        public StringifiedExpression DispatchSwitch(Expression e)
        {
            throw new NotImplementedException();
        }

        [Pure]
        public StringifiedExpression DispatchThrow(Expression e)
        {
            throw new NotImplementedException();
        }

        [Pure]
        public StringifiedExpression DispatchTry(Expression e)
        {
            throw new NotImplementedException();
        }

        [Pure]
        public StringifiedExpression DispatchUnbox(Expression e)
        {
            throw new NotImplementedException();
        }

        [Pure]
        public StringifiedExpression DispatchPowerAssign(Expression e)
        {
            throw new NotImplementedException();
        }

        [Pure]
        public StringifiedExpression DispatchIsTrue(Expression e)
        {
            throw new NotImplementedException();
        }

        [Pure]
        public StringifiedExpression DispatchIsFalse(Expression e)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
