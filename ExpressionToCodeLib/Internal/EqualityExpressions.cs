using System;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

// ReSharper disable PossiblyMistakenUseOfParamsMethod
// ReSharper disable RedundantNameQualifier
namespace ExpressionToCodeLib.Internal
{
    public enum EqualityExpressionClass
    {
        None,
        EqualsOp,
        NotEqualsOp,
        ObjectEquals,
        ObjectEqualsStatic,
        ObjectReferenceEquals,
        EquatableEquals,
        SequenceEqual,
        StructuralEquals,
    }

    public static class EqualityExpressions
    {
        static readonly MethodInfo objEqualInstanceMethod = ((Func<object, bool>)new object().Equals).GetMethodInfo();
        static readonly MethodInfo objEqualStaticMethod = ((Func<object, object, bool>)Equals).GetMethodInfo();
        static readonly MethodInfo objEqualReferenceMethod = ((Func<object, object, bool>)ReferenceEquals).GetMethodInfo();

        public static EqualityExpressionClass CheckForEquality(Expression<Func<bool>> e)
            => ExtractEqualityType(e).Item1;

        static Tuple<EqualityExpressionClass, Expression, Expression> ExtractEqualityType(Expression<Func<bool>> e)
            => ExtractEqualityType(e.Body);

        static Tuple<EqualityExpressionClass, Expression, Expression> ExtractEqualityType(Expression e)
        {
            if (e.Type == typeof(bool)) {
                if (e is BinaryExpression binExpr) {
                    if (binExpr.NodeType == ExpressionType.Equal) {
                        return Tuple.Create(EqualityExpressionClass.EqualsOp, binExpr.Left, binExpr.Right);
                    } else if (e.NodeType == ExpressionType.NotEqual) {
                        return Tuple.Create(EqualityExpressionClass.NotEqualsOp, binExpr.Left, binExpr.Right);
                    }
                } else if (e.NodeType == ExpressionType.Call) {
                    var mce = (MethodCallExpression)e;
                    if (mce.Method.Equals(((Func<object, bool>)new object().Equals).GetMethodInfo())) {
                        return Tuple.Create(EqualityExpressionClass.ObjectEquals, mce.Object, mce.Arguments.Single());
                    } else if (mce.Method.Equals(objEqualStaticMethod)) {
                        return Tuple.Create(
                            EqualityExpressionClass.ObjectEqualsStatic,
                            mce.Arguments.First(),
                            mce.Arguments.Skip(1).Single());
                    } else if (mce.Method.Equals(objEqualReferenceMethod)) {
                        return Tuple.Create(
                            EqualityExpressionClass.ObjectReferenceEquals,
                            mce.Arguments.First(),
                            mce.Arguments.Skip(1).Single());
                    } else if (IsImplementationOfGenericInterfaceMethod(mce.Method, typeof(IEquatable<>), "Equals")) {
                        return Tuple.Create(EqualityExpressionClass.EquatableEquals, mce.Object, mce.Arguments.Single());
                    } else if (IsImplementationOfInterfaceMethod(mce.Method, typeof(IStructuralEquatable), "Equals")) {
                        return Tuple.Create(EqualityExpressionClass.StructuralEquals, mce.Object, mce.Arguments.Single());
                    } else if (HaveSameGenericDefinition(
                        mce.Method,
                        ((Func<IEnumerable<int>, IEnumerable<int>, bool>)Enumerable.SequenceEqual).GetMethodInfo())) {
                        return Tuple.Create(EqualityExpressionClass.SequenceEqual, mce.Arguments.First(), mce.Arguments.Skip(1).Single());
                    }
                }
            }

            return Tuple.Create(EqualityExpressionClass.None, default(Expression), default(Expression));
        }

        static ConstantExpression ToConstantExpr(ExpressionToCodeConfiguration config, Expression e)
        {
            try {
                var func = config.Value.ExpressionCompiler.Compile(Expression.Lambda(e));
                try {
                    var val = func.DynamicInvoke();
                    return Expression.Constant(val, e.Type);
                } catch (Exception) {
                    return null; //todo:more specific?
                }
            } catch (InvalidOperationException) {
                return null;
            }
        }

        static bool? EvalBoolExpr(ExpressionToCodeConfiguration config, Expression e)
        {
            try {
                return EvalBoolLambda(config, Expression.Lambda<Func<bool>>(e));
            } catch (InvalidCastException) {
                return null;
            }
        }

        static bool? EvalBoolLambda(ExpressionToCodeConfiguration config, Expression<Func<bool>> e)
        {
            try {
                return EvalBoolFunc(config.Value.ExpressionCompiler.Compile(e));
            } catch (InvalidOperationException) {
                return null;
            }
        }

        static bool? EvalBoolFunc(Func<bool> func)
        {
            try {
                return func();
            } catch (Exception) {
                return null;
            }
        }

        public static IEnumerable<Tuple<EqualityExpressionClass, bool>> DisagreeingEqualities(ExpressionToCodeConfiguration config, Expression<Func<bool>> e)
        {
            var currEquals = ExtractEqualityType(e);
            if (currEquals.Item1 == EqualityExpressionClass.None) {
                return null;
            }

            var currVal = EvalBoolLambda(config, e);
            if (!currVal.HasValue) {
                return null;
            }

            return DisagreeingEqualities(config, currEquals.Item2, currEquals.Item3, currVal.Value);
        }

        static IEnumerable<Tuple<EqualityExpressionClass, bool>> DisagreeingEqualities(
            ExpressionToCodeConfiguration config,
            Expression left,
            Expression right,
            bool shouldBeEqual)
        {
            var leftC = ToConstantExpr(config, left);
            var rightC = ToConstantExpr(config, right);

            Tuple<EqualityExpressionClass, bool> ReportIfError(EqualityExpressionClass eqClass, bool? itsVal)
                => shouldBeEqual == itsVal ? null : Tuple.Create(eqClass, !itsVal.HasValue);

            var ienumerableTypes =
                GetGenericInterfaceImplementation(leftC.Type, typeof(IEnumerable<>))
                    .Intersect(GetGenericInterfaceImplementation(rightC.Type, typeof(IEnumerable<>)))
                    .Select(seqType => seqType.GetTypeInfo().GetGenericArguments().Single());

            var seqEqualsMethod =
                ((Func<IEnumerable<int>, IEnumerable<int>, bool>)Enumerable.SequenceEqual).GetMethodInfo().GetGenericMethodDefinition();

            var iequatableEqualsMethods =
                (from genEquatable in GetGenericInterfaceImplementation(leftC.Type, typeof(IEquatable<>))
                    let otherType = genEquatable.GetTypeInfo().GetGenericArguments().Single()
                    where otherType.GetTypeInfo().IsAssignableFrom(rightC.Type)
                    let ifacemap = leftC.Type.GetTypeInfo().GetRuntimeInterfaceMap(genEquatable)
                    select
                        ifacemap.InterfaceMethods.Zip(ifacemap.InterfaceMethods, Tuple.Create)
                            .Single(ifaceAndImpl => ifaceAndImpl.Item1.Name == "Equals")
                            .Item2).Distinct();

            var errs = new[] {
                    ReportIfError(EqualityExpressionClass.EqualsOp, EvalBoolExpr(config, Expression.Equal(leftC, rightC))),
                    ReportIfError(EqualityExpressionClass.NotEqualsOp, EvalBoolExpr(config, Expression.Not(Expression.NotEqual(leftC, rightC)))),
                    ReportIfError(
                        EqualityExpressionClass.ObjectEquals,
                        EvalBoolExpr(config, Expression.Call(leftC, objEqualInstanceMethod, Expression.Convert(rightC, typeof(object))))),
                    ReportIfError(
                        EqualityExpressionClass.ObjectEqualsStatic,
                        EvalBoolExpr(
                            config,
                            Expression.Call(
                                objEqualStaticMethod,
                                Expression.Convert(leftC, typeof(object)),
                                Expression.Convert(rightC, typeof(object))))),
                    ReportIfError(EqualityExpressionClass.ObjectReferenceEquals, ReferenceEquals(leftC.Value, rightC.Value)),
                    ReportIfError(
                        EqualityExpressionClass.StructuralEquals,
                        StructuralComparisons.StructuralEqualityComparer.Equals(leftC.Value, rightC.Value)),
                }.Concat(
                    iequatableEqualsMethods.Select(
                        method =>
                            ReportIfError(
                                EqualityExpressionClass.EquatableEquals,
                                EvalBoolExpr(
                                    config,
                                    Expression.Call(leftC, method, rightC))))
                )
                .Concat(
                    ienumerableTypes.Select(
                        elemType =>
                            ReportIfError(
                                EqualityExpressionClass.SequenceEqual,
                                EvalBoolExpr(
                                    config,
                                    Expression.Call(seqEqualsMethod.MakeGenericMethod(elemType), leftC, rightC))))
                );
            return errs.Where(err => err != null).Distinct().ToArray();
        }

        static bool HaveSameGenericDefinition(MethodInfo a, MethodInfo b)
            => a.IsGenericMethod && b.IsGenericMethod
                && a.GetGenericMethodDefinition().Equals(b.GetGenericMethodDefinition());

        static bool IsImplementationOfGenericInterfaceMethod(
            MethodInfo method,
            Type genericInterfaceType,
            string methodName)
            => GetGenericInterfaceImplementation(method.DeclaringType, genericInterfaceType)
                    .Any(constructedInterfaceType => IsImplementationOfInterfaceMethod(method, constructedInterfaceType, methodName))
                || method.DeclaringType.GetTypeInfo().IsInterface && method.Name == methodName && method.DeclaringType.GetTypeInfo().IsGenericType
                && method.DeclaringType?.GetGenericTypeDefinition() == genericInterfaceType;

        static bool IsImplementationOfInterfaceMethod(MethodInfo method, Type interfaceType, string methodName)
        {
            if (!interfaceType.GetTypeInfo().IsAssignableFrom(method.DeclaringType)) {
                return false;
            }

            var interfaceMap = method.DeclaringType.GetTypeInfo().GetRuntimeInterfaceMap(interfaceType);
            return interfaceMap.InterfaceMethods.Where((t, i) => t.Name == methodName && method.Equals(interfaceMap.TargetMethods[i]))
                .Any();
        }

        static IEnumerable<Type> GetGenericInterfaceImplementation(Type type, Type genericInterfaceType)
            => from itype in type.GetTypeInfo().GetInterfaces()
                where itype.GetTypeInfo().IsGenericType && itype.GetGenericTypeDefinition() == genericInterfaceType
                select itype;
    }
}
