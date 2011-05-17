using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

// ReSharper disable RedundantNameQualifier

namespace ExpressionToCodeLib {
	public enum EqualityExpressionClass {
		None, EqualsOp, NotEqualsOp, ObjectEquals, ObjectEqualsStatic, ObjectReferenceEquals,
		EquatableEquals,
		SequenceEqual
#if DOTNET40
, StructuralEquals
#endif
	}

	public static class EqualityExpressions {


		readonly static MethodInfo objEqualInstanceMethod = ((Func<object, bool>)new object().Equals).Method;
		readonly static MethodInfo objEqualStaticMethod = ((Func<object, object, bool>)object.Equals).Method;
		readonly static MethodInfo objEqualReferenceMethod = ((Func<object, object, bool>)object.ReferenceEquals).Method;

		public static EqualityExpressionClass CheckForEquality(Expression<Func<bool>> e) { return ExtractEqualityType(e).Item1; }
		public static Tuple<EqualityExpressionClass, Expression, Expression> ExtractEqualityType(Expression<Func<bool>> e) { return ExtractEqualityType(e.Body); }
		public static Tuple<EqualityExpressionClass, Expression, Expression> ExtractEqualityType(Expression e) {
			if (e.Type.Equals(typeof(bool))) {
				if (e is BinaryExpression) {
					var binExpr = (BinaryExpression)e;
					if (binExpr.NodeType == ExpressionType.Equal)
						return Tuple.Create(EqualityExpressionClass.EqualsOp, binExpr.Left, binExpr.Right);
					else if (e.NodeType == ExpressionType.NotEqual)
						return Tuple.Create(EqualityExpressionClass.NotEqualsOp, binExpr.Left, binExpr.Right);
				} else if (e.NodeType == ExpressionType.Call) {
					MethodCallExpression mce = (MethodCallExpression)e;
					if (mce.Method.Equals(((Func<object, bool>)new object().Equals).Method))
						return Tuple.Create(EqualityExpressionClass.ObjectEquals, mce.Object, mce.Arguments.Single());
					else if (mce.Method.Equals(objEqualStaticMethod))
						return Tuple.Create(EqualityExpressionClass.ObjectEqualsStatic, mce.Arguments.First(), mce.Arguments.Skip(1).Single());
					else if (mce.Method.Equals(objEqualReferenceMethod))
						return Tuple.Create(EqualityExpressionClass.ObjectReferenceEquals, mce.Arguments.First(), mce.Arguments.Skip(1).Single());
					else if (IsImplementationOfGenericInterfaceMethod(mce.Method, typeof(IEquatable<>), "Equals"))
						return Tuple.Create(EqualityExpressionClass.EquatableEquals, mce.Object, mce.Arguments.Single());
#if DOTNET40
					else if (IsImplementationOfInterfaceMethod(mce.Method, typeof(IStructuralEquatable), "Equals"))
						return Tuple.Create(EqualityExpressionClass.StructuralEquals, mce.Object, mce.Arguments.Single());
#endif
					else if (HaveSameGenericDefinition(mce.Method, ((Func<IEnumerable<int>, IEnumerable<int>, bool>)Enumerable.SequenceEqual).Method))
						return Tuple.Create(EqualityExpressionClass.SequenceEqual, mce.Arguments.First(), mce.Arguments.Skip(1).Single());
				}
			}
			return Tuple.Create(EqualityExpressionClass.None, default(Expression), default(Expression));
		}


		static ConstantExpression ToConstantExpr(Expression e) {
			try {
				Delegate func = Expression.Lambda(e).Compile();
				try {
					object val = func.DynamicInvoke();
					return Expression.Constant(val, e.Type);
				} catch (Exception) {
					return null;//todo:more specific?
				}
			} catch (InvalidOperationException) {
				return null;
			}
		}

		static bool? EvalBoolExpr(Expression e) { try { return EvalBoolLambda(Expression.Lambda<Func<bool>>(e)); } catch (InvalidCastException) { return null; } }
		static bool? EvalBoolLambda(Expression<Func<bool>> e) { try { return EvalBoolFunc(e.Compile()); } catch (InvalidOperationException) { return null; } }
		static bool? EvalBoolFunc(Func<bool> func) { try { return func(); } catch (Exception) { return null; } }

		public static IEnumerable<Tuple<EqualityExpressionClass, bool>> DisagreeingEqualities(Expression<Func<bool>> e) {
			var currEquals = ExtractEqualityType(e);
			if (currEquals.Item1 == EqualityExpressionClass.None) return null;
			var currVal = EvalBoolLambda(e);
			if (!currVal.HasValue) return null;
			return DisagreeingEqualities(currEquals.Item2, currEquals.Item3, currVal.Value);
		}


		public static IEnumerable<Tuple<EqualityExpressionClass, bool>> DisagreeingEqualities(Expression left, Expression right, bool shouldBeEqual) {
			var leftC = ToConstantExpr(left);
			var rightC = ToConstantExpr(right);
			Func<EqualityExpressionClass, bool?, Tuple<EqualityExpressionClass, bool>> reportIfError =
				(eqClass, itsVal) => shouldBeEqual == itsVal ? null : Tuple.Create(eqClass, !itsVal.HasValue);

			var ienumerableTypes =
				GetGenericInterfaceImplementation(leftC.Type, typeof(IEnumerable<>))
					.Intersect(GetGenericInterfaceImplementation(rightC.Type, typeof(IEnumerable<>)))
					.Select(seqType => seqType.GetGenericArguments().Single());

			var seqEqualsMethod = ((Func<IEnumerable<int>, IEnumerable<int>, bool>)Enumerable.SequenceEqual).Method.GetGenericMethodDefinition();

			var iequatableEqualsMethods =
				(from genEquatable in GetGenericInterfaceImplementation(leftC.Type, typeof(IEquatable<>))
				 let otherType = genEquatable.GetGenericArguments().Single()
				 where otherType.IsAssignableFrom(rightC.Type)
				 let ifacemap = leftC.Type.GetInterfaceMap(genEquatable)
				 select ifacemap.InterfaceMethods.Zip(ifacemap.InterfaceMethods, Tuple.Create).Single(ifaceAndImpl => ifaceAndImpl.Item1.Name == "Equals").Item2).Distinct();


			var errs = new[]{
			 reportIfError(EqualityExpressionClass.EqualsOp, EvalBoolExpr(Expression.Equal(leftC,rightC))),
			 reportIfError(EqualityExpressionClass.NotEqualsOp, EvalBoolExpr(Expression.Not(Expression.NotEqual(leftC,rightC)))),
			 reportIfError(EqualityExpressionClass.ObjectEquals, EvalBoolExpr(Expression.Call(leftC,objEqualInstanceMethod,rightC))),
			 reportIfError(EqualityExpressionClass.ObjectEqualsStatic, EvalBoolExpr(Expression.Call(objEqualStaticMethod,leftC,rightC))),
			 reportIfError(EqualityExpressionClass.ObjectReferenceEquals, object.ReferenceEquals(leftC.Value, rightC.Value)),
#if DOTNET40
			 reportIfError(EqualityExpressionClass.StructuralEquals, StructuralComparisons.StructuralEqualityComparer.Equals(leftC.Value,rightC.Value)),
#endif
			}.Concat(
				iequatableEqualsMethods.Select(method =>
					reportIfError(EqualityExpressionClass.EquatableEquals, EvalBoolExpr(
						Expression.Call(leftC, method, rightC))))
			).Concat(
				ienumerableTypes.Select(elemType =>
					reportIfError(EqualityExpressionClass.SequenceEqual, EvalBoolExpr(
						Expression.Call(seqEqualsMethod.MakeGenericMethod(elemType), leftC, rightC))))
			);
			return errs.Where(err => err != null).Distinct().ToArray();
		}


		static bool HaveSameGenericDefinition(MethodInfo a, MethodInfo b) {
			return a.IsGenericMethod && b.IsGenericMethod && a.GetGenericMethodDefinition().Equals(b.GetGenericMethodDefinition());
		}


		static bool IsImplementationOfGenericInterfaceMethod(MethodInfo method, Type genericInterfaceType, string methodName) {
			return
				GetGenericInterfaceImplementation(method.DeclaringType, genericInterfaceType)
				.Any(constructedInterfaceType => IsImplementationOfInterfaceMethod(method, constructedInterfaceType, methodName))
				|| method.DeclaringType.IsInterface && method.Name == methodName && method.DeclaringType.IsGenericType && method.DeclaringType.GetGenericTypeDefinition() == genericInterfaceType;
		}

		static bool IsImplementationOfInterfaceMethod(MethodInfo method, Type interfaceType, string methodName) {
			if (!interfaceType.IsAssignableFrom(method.DeclaringType)) return false;
			var interfaceMap = method.DeclaringType.GetInterfaceMap(interfaceType);
			return interfaceMap.InterfaceMethods.Where((t, i) => t.Name == methodName && method.Equals(interfaceMap.TargetMethods[i])).Any();
		}

		static IEnumerable<Type> GetGenericInterfaceImplementation(Type type, Type genericInterfaceType) {
			return
					from itype in type.GetInterfaces()
					where itype.IsGenericType && itype.GetGenericTypeDefinition().Equals(genericInterfaceType)
					select itype
				;
		}
	}
}
