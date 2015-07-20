using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Reflection;
using ExpressionToCodeLib.Unstable_v2_Api;

namespace ExpressionToCodeLib
{
    class ExpressionToCodeImpl : IExpressionTypeDispatch
    {
        #region General Helpers
        readonly IObjectToCode objectToCode;
        readonly bool explicitMethodTypeArgs;
        readonly Action<ExprTextPart, int> sink;
        int Depth;

        //TODO: refactor IExpressionTypeDispatch into an input/output model to avoid this tricky side-effect approach.
        internal ExpressionToCodeImpl(IObjectToCode objectToCode, bool explicitMethodTypeArgs, Action<ExprTextPart, int> sink)
        {
            this.objectToCode = objectToCode;
            this.explicitMethodTypeArgs = explicitMethodTypeArgs;
            this.sink = sink;
        }

        internal ExpressionToCodeImpl(Action<ExprTextPart, int> sink)
            : this(ObjectStringify.Default, false, sink) { }

        void Sink(string text) { sink(ExprTextPart.TextOnly(text), Depth); }
        void Sink(string text, Expression value) { sink(ExprTextPart.TextAndExpr(text, value), Depth); }

        void NestExpression(ExpressionType? parentType, Expression child, bool parensIfEqualRank = false)
        {
            int parentRank = parentType == null ? 0 : ExpressionPrecedence.Rank(parentType.Value);
            bool needsParens = parentRank > 0
                && (parensIfEqualRank ? parentRank - 1 : parentRank) < ExpressionPrecedence.Rank(child.NodeType);
            if(needsParens) {
                sink(ExprTextPart.TextOnly("("), Depth);
            }
            RawChildDispatch(child);
            if(needsParens) {
                sink(ExprTextPart.TextOnly(")"), Depth);
            }
        }

        void RawChildDispatch(Expression child) { RawChildDispatch(new Argument { Expr = child }); }

        void RawChildDispatch(Argument child)
        {
            Depth++;
            if(child.PrefixOrNull != null) {
                Sink(child.PrefixOrNull);
            }
            this.ExpressionDispatch(child.Expr);
            Depth--;
        }

        void JoinDispatch<T>(IEnumerable<T> children, string joiner, Action<T> childVisitor)
        {
            bool isFirst = true;
            foreach(var child in children) {
                if(!isFirst) {
                    Sink(joiner);
                }
                childVisitor(child);
                isFirst = false;
            }
        }

        void JoinDispatch(IEnumerable<Argument> children, string joiner) { JoinDispatch(children, joiner, RawChildDispatch); }

        struct Argument
        {
            public Expression Expr;
            public string PrefixOrNull;
        }

        void ArgListDispatch(
            IEnumerable<Argument> arguments,
            Expression value = null,
            string open = "(",
            string close = ")",
            string joiner = ", ")
        {
            if(value != null) {
                Sink(open, value);
            } else {
                Sink(open);
            }
            JoinDispatch(arguments, joiner);
            Sink(close);
        }

        void BinaryDispatch(string op, Expression e)
        {
            var be = (BinaryExpression)e;
            Expression left, right;
            UnwrapEnumOp(be, out left, out right);
            NestExpression(be.NodeType, left);
            Sink(" " + op + " ", e);
            NestExpression(be.NodeType, right, true);
        }

        static void UnwrapEnumOp(BinaryExpression be, out Expression left, out Expression right)
        {
            left = be.Left;
            right = be.Right;
            var uleft = left.NodeType == ExpressionType.Convert ? ((UnaryExpression)left).Operand : null;
            var uright = right.NodeType == ExpressionType.Convert ? ((UnaryExpression)right).Operand : null;
            if(uleft != null) {
                if(uright != null) {
                    if(uright.Type.EnusureNullability() == uleft.Type.EnusureNullability()) {
                        left = uleft;
                        right = uright;
                    }
                } else {
                    UnwrapEnumBinOp(uleft, ref left, ref right);
                }
            } else
                //uleft != null
                if(uright != null) {
                    UnwrapEnumBinOp(uright, ref right, ref left);
                }
        }

        static void UnwrapEnumBinOp(Expression expr1uncast, ref Expression expr1, ref Expression expr2)
        {
            Type expr1nonnullableType = expr1uncast.Type.AvoidNullability();
            Type expr2nonnullableType = expr2.Type.AvoidNullability();
            if(expr1nonnullableType.IsEnum
                && expr1nonnullableType.GetEnumUnderlyingType() == expr2nonnullableType
                || expr1nonnullableType == typeof(char) && expr2nonnullableType == typeof(int)
                ) {
                expr1 = expr1uncast;

                if(expr2.NodeType == ExpressionType.Constant) {
                    object value = ((ConstantExpression)expr2).Value;
                    if(value == null) {
                        expr2 = Expression.Default(expr1uncast.Type.EnusureNullability());
                    } else if(expr1nonnullableType == typeof(char)) {
                        expr2 = Expression.Constant((char)(int)value);
                    } else {
                        expr2 = Expression.Constant(Enum.ToObject(expr1nonnullableType, value));
                    }
                } else {
                    expr2 = Expression.Convert(expr2, expr1uncast.Type);
                }
            }
        }

        void UnaryDispatch(string op, Expression e)
        {
            var ue = (UnaryExpression)e;
            bool needsSpace = ExpressionPrecedence.TokenizerConfusable(ue.NodeType, ue.Operand.NodeType);
            Sink(op + (needsSpace ? " " : ""), e);
            NestExpression(ue.NodeType, ue.Operand);
        }

        void UnaryDispatchConvert(Expression e)
        {
            var ue = (UnaryExpression)e;
            if(e.Type.IsAssignableFrom(ue.Operand.Type)) // base class, basically; don't re-print identical values.
            {
                Sink("(" + objectToCode.TypeNameToCode(e.Type) + ")");
            } else {
                Sink("(" + objectToCode.TypeNameToCode(e.Type) + ")", e);
            }
            NestExpression(ue.NodeType, ue.Operand);
        }

        void UnaryPostfixDispatch(string op, Expression e)
        {
            var ue = (UnaryExpression)e;
            NestExpression(ue.NodeType, ue.Operand);
            Sink(op, e);
        }

        void TypeOpDispatch(string op, Expression e)
        {
            NestExpression(e.NodeType, ((TypeBinaryExpression)e).Expression);
            Sink(" " + op + " ", e);
            Sink(objectToCode.TypeNameToCode(((TypeBinaryExpression)e).TypeOperand));
        }

        void StatementDispatch(Expression e, ExpressionType? parentType = null)
        {
            NestExpression(parentType, e);
            Sink("; ");
        }

        void StatementDispatch(String prefix, Expression e, ExpressionType? parentType = null)
        {
            Sink(prefix);
            Sink(" ");
            StatementDispatch(e, parentType);
        }
        #endregion

        #region Hard Cases
        public void DispatchLambda(Expression e)
        {
            var le = (LambdaExpression)e;
            if(le.Parameters.Count == 1) {
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

        public void DispatchMemberAccess(Expression e)
        {
            var me = (MemberExpression)e;
            Expression memberOfExpr = me.Expression;
            if(memberOfExpr != null && !isThisRef(memberOfExpr) && !isClosureRef(memberOfExpr)) {
                NestExpression(e.NodeType, memberOfExpr);
                Sink(".");
            } else if(ReflectionHelpers.IsMemberInfoStatic(me.Member)) {
                Sink(objectToCode.TypeNameToCode(me.Member.ReflectedType) + ".");
            }

            Sink(me.Member.Name, e);
        }

        static readonly MethodInfo createDelegate = typeof(Delegate).GetMethod(
            "CreateDelegate",
            new[] { typeof(Type), typeof(object), typeof(MethodInfo) });


        public void DispatchCall(Expression e)
        {
            var mce = (MethodCallExpression)e;

            var optPropertyInfo = ReflectionHelpers.GetPropertyIfGetter(mce.Method);
            if(optPropertyInfo != null
                && (optPropertyInfo.Name == "Item"
                    || mce.Object.Type == typeof(string) && optPropertyInfo.Name == "Chars")) {
                NestExpression(mce.NodeType, mce.Object);
                //indexers don't support ref/out; so we can use unprefixed arguments
                ArgListDispatch(GetArgumentsForMethod(mce.Method, mce.Arguments), mce, "[", "]");
            } else if(mce.Method.Equals(createDelegate) && mce.Arguments.Count == 3
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

        void SinkMethodName(MethodCallExpression mce, MethodInfo method, Expression objExpr)
        {
            if(objExpr != null) {
                if(!(isThisRef(objExpr) || isClosureRef(objExpr))) {
                    NestExpression(mce.NodeType, objExpr);
                    Sink(".");
                }
            } else if(method.IsStatic) {
                Sink(objectToCode.TypeNameToCode(method.DeclaringType) + "."); //TODO:better reference avoiding for this?
            }
            var methodName = method.Name;

            methodName += CreateGenericArgumentsIfNecessary(mce, method);
            Sink(methodName, mce);
        }

        string CreateGenericArgumentsIfNecessary(MethodCallExpression mce, MethodInfo method)
        {
            if(!method.IsGenericMethod) {
                return "";
            }

            if(!explicitMethodTypeArgs) {
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

                if(!confusibleOverloads.Any()
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

        public void DispatchIndex(Expression e)
        {
            var ie = (IndexExpression)e;
            NestExpression(ie.NodeType, ie.Object);

			var args = ie.Indexer == null ?
							ie.Arguments.Select(a => new Argument
							{
								Expr = a,
								PrefixOrNull = null
							})
						: // else {
							GetArgumentsForMethod(ie.Indexer.GetIndexParameters(), ie.Arguments);

            ArgListDispatch(args, ie, "[", "]");
        }

        public void DispatchInvoke(Expression e)
        {
            var ie = (InvocationExpression)e;
            if(ie.Expression.NodeType == ExpressionType.Lambda) {
                Sink("new " + objectToCode.TypeNameToCode(ie.Expression.Type));
            }
            NestExpression(ie.NodeType, ie.Expression);
            var invokeMethod = ie.Expression.Type.GetMethod("Invoke");
            var args = GetArgumentsForMethod(invokeMethod, ie.Arguments);
            ArgListDispatch(args, ie);
        }

        public void DispatchConstant(Expression e)
        {
            var const_Val = ((ConstantExpression)e).Value;
            string codeRepresentation = objectToCode.PlainObjectToCode(const_Val, e.Type);
            //e.Type.IsVisible
            if(codeRepresentation == null) {
                var typeclass = e.Type.GuessTypeClass();
                if(typeclass == ReflectionHelpers.TypeClass.NormalType) // probably this!
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

        public void DispatchConditional(Expression e)
        {
            var ce = (ConditionalExpression)e;
            NestExpression(ce.NodeType, ce.Test);
            Sink(" ? ", e);
            NestExpression(ce.NodeType, ce.IfTrue);
            Sink(" : ");
            NestExpression(ce.NodeType, ce.IfFalse);
        }

        public void DispatchListInit(Expression e)
        {
            var lie = (ListInitExpression)e;
            Sink("new ", lie);
            Sink(objectToCode.TypeNameToCode(lie.NewExpression.Constructor.ReflectedType));
            if(lie.NewExpression.Arguments.Any()) {
                ArgListDispatch(GetArgumentsForMethod(lie.NewExpression.Constructor, lie.NewExpression.Arguments));
            }

            Sink(" { ");
            JoinDispatch(lie.Initializers, ", ", DispatchElementInit);
            Sink(" }");
        }

        void DispatchElementInit(ElementInit elemInit)
        {
            if(elemInit.Arguments.Count != 1) {
                ArgListDispatch(elemInit.Arguments.Select(ae => new Argument { Expr = ae }), null, "{ ", " }"); //??
            } else {
                RawChildDispatch(elemInit.Arguments.Single());
            }
        }

        void DispatchMemberBinding(MemberBinding mb)
        {
            Sink(mb.Member.Name + " = ");
            if(mb is MemberMemberBinding) {
                var mmb = (MemberMemberBinding)mb;
                Sink("{ ");
                JoinDispatch(mmb.Bindings, ", ", DispatchMemberBinding);
                Sink(" }");
            } else if(mb is MemberListBinding) {
                var mlb = (MemberListBinding)mb;
                Sink("{ ");
                JoinDispatch(mlb.Initializers, ", ", DispatchElementInit);
                Sink(" }");
            } else if(mb is MemberAssignment) {
                RawChildDispatch(((MemberAssignment)mb).Expression);
            } else {
                throw new NotImplementedException("Member binding of unknown type: " + mb.GetType());
            }
        }

        public void DispatchMemberInit(Expression e)
        {
            var mie = (MemberInitExpression)e;
            Sink("new ", mie);
            Sink(objectToCode.TypeNameToCode(mie.NewExpression.Constructor.ReflectedType));
            if(mie.NewExpression.Arguments.Any()) {
                ArgListDispatch(GetArgumentsForMethod(mie.NewExpression.Constructor, mie.NewExpression.Arguments));
            }

            Sink(" { ");
            JoinDispatch(mie.Bindings, ", ", DispatchMemberBinding);
            Sink(" }");
        }

        public void DispatchNew(Expression e)
        {
            var ne = (NewExpression)e;
            if(ne.Type.GuessTypeClass() == ReflectionHelpers.TypeClass.AnonymousType) {
                var parms = ne.Type.GetConstructors().Single().GetParameters();
                var props = ne.Type.GetProperties();
                if(
                    !parms.Select(p => new { p.Name, Type = p.ParameterType })
                        .SequenceEqual(props.Select(p => new { p.Name, Type = p.PropertyType }))) {
                    throw new InvalidOperationException(
                        "Constructor params for anonymous type don't match it's properties!");
                }
                if(!parms.Select(p => p.ParameterType).SequenceEqual(ne.Arguments.Select(argE => argE.Type))) {
                    throw new InvalidOperationException(
                        "Constructor Arguments for anonymous type don't match it's type signature!");
                }
                Sink("new { ");
                for(int i = 0; i < props.Length; i++) {
                    Sink(props[i].Name + " = ");
                    RawChildDispatch(ne.Arguments[i]);
                    if(i + 1 < props.Length) {
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

        public void DispatchNewArrayInit(Expression e)
        {
            var nae = (NewArrayExpression)e;
            Type arrayElemType = nae.Type.GetElementType();
            bool isDelegate = typeof(Delegate).IsAssignableFrom(arrayElemType);
            bool implicitTypeOK = !isDelegate && nae.Expressions.Any()
                && nae.Expressions.All(expr => expr.Type == arrayElemType);
            Sink("new" + (implicitTypeOK ? "" : " " + objectToCode.TypeNameToCode(arrayElemType)) + "[] ", nae);
            ArgListDispatch(nae.Expressions.Select(e1 => new Argument { Expr = e1 }), null, "{ ", " }");
        }

        public void DispatchNewArrayBounds(Expression e)
        {
            var nae = (NewArrayExpression)e;
            Type arrayElemType = nae.Type.GetElementType();
            Sink("new " + objectToCode.TypeNameToCode(arrayElemType), nae);
            ArgListDispatch(nae.Expressions.Select(e1 => new Argument { Expr = e1 }), null, "[", "]");
        }

        public void DispatchBlock(Expression e)
        {
            var be = (BlockExpression)e;
            bool hasReturn = (be.Type != typeof(void));
            var statements = hasReturn ? be.Expressions.Take(be.Expressions.Count - 1) : be.Expressions;

            Sink("{ ");

            foreach(var v in be.Variables) {
                StatementDispatch(objectToCode.TypeNameToCode(v.Type), v, ExpressionType.Block);
            }

            foreach(var child in statements) {
                StatementDispatch(child, ExpressionType.Block);
            }

            if(hasReturn) {
                StatementDispatch("return", be.Result, ExpressionType.Block);
            }

            Sink("}");
        }
        #endregion

        #region Easy Cases
        public void DispatchPower(Expression e)
        {
            Sink("Math.Pow", e);
            var binaryExpression = (BinaryExpression)e;
            ArgListDispatch(new[] { binaryExpression.Left, binaryExpression.Right }.Select(e1 => new Argument { Expr = e1 }));
        }

        public void DispatchAdd(Expression e) { BinaryDispatch("+", e); }
        public void DispatchAddChecked(Expression e) { BinaryDispatch("+", e); } //TODO: checked
        public void DispatchAnd(Expression e) { BinaryDispatch("&", e); }
        public void DispatchAndAlso(Expression e) { BinaryDispatch("&&", e); }

        public void DispatchArrayLength(Expression e)
        {
            NestExpression(e.NodeType, ((UnaryExpression)e).Operand);
            Sink(".Length", e);
        }

        public void DispatchArrayIndex(Expression e)
        {
            var binaryExpression = (BinaryExpression)e;
            NestExpression(e.NodeType, binaryExpression.Left);
            Sink("[", e);
            NestExpression(null, binaryExpression.Right);
            Sink("]");
        }

        public void DispatchCoalesce(Expression e) { BinaryDispatch("??", e); }
        public void DispatchConvert(Expression e) { UnaryDispatchConvert(e); }
        public void DispatchConvertChecked(Expression e) { UnaryDispatchConvert(e); }
        //TODO: get explicit and implicit conversion operators right.
        public void DispatchDivide(Expression e) { BinaryDispatch("/", e); }
        public void DispatchEqual(Expression e) { BinaryDispatch("==", e); }
        public void DispatchExclusiveOr(Expression e) { BinaryDispatch("^", e); }
        public void DispatchGreaterThan(Expression e) { BinaryDispatch(">", e); }
        public void DispatchGreaterThanOrEqual(Expression e) { BinaryDispatch(">=", e); }
        public void DispatchLeftShift(Expression e) { BinaryDispatch("<<", e); }
        public void DispatchLessThan(Expression e) { BinaryDispatch("<", e); }
        public void DispatchLessThanOrEqual(Expression e) { BinaryDispatch("<=", e); }
        public void DispatchModulo(Expression e) { BinaryDispatch("%", e); }
        public void DispatchMultiply(Expression e) { BinaryDispatch("*", e); }
        public void DispatchMultiplyChecked(Expression e) { BinaryDispatch("*", e); }
        public void DispatchNegate(Expression e) { UnaryDispatch("-", e); }
        public void DispatchUnaryPlus(Expression e) { UnaryDispatch("+", e); }
        public void DispatchNegateChecked(Expression e) { UnaryDispatch("-", e); }
        public void DispatchNot(Expression e) { UnaryDispatch(e.Type == typeof(bool) || e.Type == typeof(bool?) ? "!" : "~", e); }
        public void DispatchNotEqual(Expression e) { BinaryDispatch("!=", e); }
        public void DispatchOr(Expression e) { BinaryDispatch("|", e); }
        public void DispatchOrElse(Expression e) { BinaryDispatch("||", e); }

        public void DispatchParameter(Expression e)
        {
            var parameterExpression = ((ParameterExpression)e);
            Sink(parameterExpression.Name ?? (parameterExpression.Type.Name + parameterExpression.GetHashCode()), e);
        }

        public void DispatchQuote(Expression e) { NestExpression(e.NodeType, ((UnaryExpression)e).Operand); }
        public void DispatchRightShift(Expression e) { BinaryDispatch(">>", e); }
        public void DispatchSubtract(Expression e) { BinaryDispatch("-", e); }
        public void DispatchSubtractChecked(Expression e) { BinaryDispatch("-", e); }
        public void DispatchTypeAs(Expression e) { UnaryPostfixDispatch(" as " + objectToCode.TypeNameToCode(e.Type), e); }
        public void DispatchTypeIs(Expression e) { TypeOpDispatch("is", e); }
        public void DispatchAssign(Expression e) { BinaryDispatch("=", e); }
        public void DispatchDecrement(Expression e) { UnaryPostfixDispatch(" - 1", e); }
        public void DispatchIncrement(Expression e) { UnaryPostfixDispatch(" + 1", e); }
        public void DispatchAddAssign(Expression e) { BinaryDispatch("+=", e); }
        public void DispatchAndAssign(Expression e) { BinaryDispatch("&=", e); }
        public void DispatchDivideAssign(Expression e) { BinaryDispatch("/=", e); }
        public void DispatchExclusiveOrAssign(Expression e) { BinaryDispatch("^=", e); }
        public void DispatchLeftShiftAssign(Expression e) { BinaryDispatch("<<=", e); }
        public void DispatchModuloAssign(Expression e) { BinaryDispatch("%=", e); }
        public void DispatchMultiplyAssign(Expression e) { BinaryDispatch("*=", e); }
        public void DispatchOrAssign(Expression e) { BinaryDispatch("|=", e); }
        public void DispatchRightShiftAssign(Expression e) { BinaryDispatch(">>=", e); }
        public void DispatchSubtractAssign(Expression e) { BinaryDispatch("-=", e); }
        public void DispatchAddAssignChecked(Expression e) { BinaryDispatch("+=", e); }
        public void DispatchMultiplyAssignChecked(Expression e) { BinaryDispatch("*=", e); }
        public void DispatchSubtractAssignChecked(Expression e) { BinaryDispatch("-=", e); }
        public void DispatchPreIncrementAssign(Expression e) { UnaryDispatch("++", e); }
        public void DispatchPreDecrementAssign(Expression e) { UnaryDispatch("--", e); }
        public void DispatchPostIncrementAssign(Expression e) { UnaryPostfixDispatch("++ ", e); }
        public void DispatchPostDecrementAssign(Expression e) { UnaryPostfixDispatch("-- ", e); }
        public void DispatchOnesComplement(Expression e) { UnaryDispatch("~", e); }
        #endregion

        #region Unused by C#'s expression support; or unavailable in the language at all.
        public void DispatchTypeEqual(Expression e) { throw new NotImplementedException(); }
        public void DispatchDebugInfo(Expression e) { throw new NotImplementedException(); }
        public void DispatchDynamic(Expression e) { throw new NotImplementedException(); }

        public void DispatchDefault(Expression e)
        {
            var defExpr = (DefaultExpression)e;

            Sink("default(" + objectToCode.TypeNameToCode(defExpr.Type) + ")");
        }

        public void DispatchExtension(Expression e) { throw new NotImplementedException(); }
        public void DispatchGoto(Expression e) { throw new NotImplementedException(); }
        public void DispatchLabel(Expression e) { throw new NotImplementedException(); }
        public void DispatchRuntimeVariables(Expression e) { throw new NotImplementedException(); }
        public void DispatchLoop(Expression e) { throw new NotImplementedException(); }
        public void DispatchSwitch(Expression e) { throw new NotImplementedException(); }
        public void DispatchThrow(Expression e) { throw new NotImplementedException(); }
        public void DispatchTry(Expression e) { throw new NotImplementedException(); }
        public void DispatchUnbox(Expression e) { throw new NotImplementedException(); }
        public void DispatchPowerAssign(Expression e) { throw new NotImplementedException(); }
        public void DispatchIsTrue(Expression e) { throw new NotImplementedException(); }
        public void DispatchIsFalse(Expression e) { throw new NotImplementedException(); }
        #endregion
    }
}
