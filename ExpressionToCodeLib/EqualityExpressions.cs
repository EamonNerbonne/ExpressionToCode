using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
// ReSharper disable RedundantNameQualifier

namespace ExpressionToCodeLib {
	public enum EqualityExpressionClass {
		None, EqualsOp, NotEqualsOp, ObjectEquals, ObjectEqualsStatic,
		EquatableEquals,
		SequenceEquals
#if DOTNET40
, StructuralEquals
#endif
	}

	public class EqualityExpressions {

		public static EqualityExpressionClass CheckForEquality(Expression<Func<bool>> e) {
			return CheckForEquality(e.Body).Item1;
		}

		readonly static MethodInfo objEqualInstanceMethod = ((Func<object, bool>)new object().Equals).Method;
		readonly static MethodInfo objEqualStaticMethod = ((Func<object, object, bool>)object.Equals).Method;


		public static Tuple<EqualityExpressionClass, Expression, Expression> CheckForEquality(Expression e) {
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
					else if (mce.Method.Equals(((Func<object, object, bool>)object.Equals).Method))
						return Tuple.Create(EqualityExpressionClass.ObjectEqualsStatic, mce.Arguments.First(), mce.Arguments.Skip(1).Single());
					else if (IsImplementationOfGenericInterfaceMethod(mce.Method, typeof(IEquatable<>), "Equals"))
						return Tuple.Create(EqualityExpressionClass.EquatableEquals, mce.Object, mce.Arguments.Single());
					else if (IsImplementationOfInterfaceMethod(mce.Method, typeof(IStructuralEquatable), "Equals"))
						return Tuple.Create(EqualityExpressionClass.StructuralEquals, mce.Object, mce.Arguments.Single());
					else if (HaveSameGenericDefinition(mce.Method, ((Func<IEnumerable<int>, IEnumerable<int>, bool>)Enumerable.SequenceEqual).Method))
						return Tuple.Create(EqualityExpressionClass.SequenceEquals, mce.Arguments.First(), mce.Arguments.Skip(1).Single());
				}
			}
			return Tuple.Create(EqualityExpressionClass.None, default(Expression), default(Expression));
		}

		public static ConstantExpression ToConstantExpr(Expression e) {
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

		public static bool? EvalBooleanExpr(Expression e) {
			try {
				Delegate func = Expression.Lambda(e).Compile();
				try {
					return (bool)func.DynamicInvoke();
				} catch (Exception) {
					return null;//todo:more specific?
				}
			} catch (InvalidOperationException) {
				return null;
			}
		}

		public static IEnumerable<Tuple<EqualityExpressionClass, bool>> DisagreeingEqualities(Expression left, Expression right, bool shouldBeEqual) {
			var leftC = ToConstantExpr(left);
			var rightC = ToConstantExpr(right);
			Func<EqualityExpressionClass, bool?, Tuple<EqualityExpressionClass, bool>> reportIfError =
				(eqClass, itsVal) => shouldBeEqual == itsVal ? null : Tuple.Create(eqClass, !itsVal.HasValue);

			var ienumerableTypes =
				GetGenericInterfaceImplementation(leftC.Type, typeof(IEnumerable<>))
					.Intersect(GetGenericInterfaceImplementation(rightC.Type, typeof(IEnumerable<>)))
					.Select(seqType=> seqType.GetGenericArguments().Single());

			var seqEqualsMethod = ((Func<IEnumerable<int>, IEnumerable<int>, bool>)Enumerable.SequenceEqual).Method.GetGenericMethodDefinition();


			var errs = new[]{
			 reportIfError(EqualityExpressionClass.EqualsOp, EvalBooleanExpr(Expression.Equal(leftC,rightC))),
			 reportIfError(EqualityExpressionClass.NotEqualsOp, EvalBooleanExpr(Expression.Not(Expression.NotEqual(leftC,rightC)))),
			 reportIfError(EqualityExpressionClass.ObjectEquals, EvalBooleanExpr(Expression.Call(leftC,objEqualInstanceMethod,rightC))),
			 reportIfError(EqualityExpressionClass.ObjectEqualsStatic, EvalBooleanExpr(Expression.Call(objEqualStaticMethod,leftC,rightC))),
			}.Concat(
				ienumerableTypes.Select(elemType=>
					reportIfError(EqualityExpressionClass.SequenceEquals, EvalBooleanExpr(
						Expression.Call(seqEqualsMethod.MakeGenericMethod( elemType),leftC,rightC))))
			);
			return errs.Where(err => err != null).Distinct().ToArray();
		}




		static bool HaveSameGenericDefinition(MethodInfo a, MethodInfo b) {
			return a.IsGenericMethod && b.IsGenericMethod && a.GetGenericMethodDefinition().Equals(b.GetGenericMethodDefinition());
		}


		static bool IsImplementationOfGenericInterfaceMethod(MethodInfo method, Type genericInterfaceType, string methodName) {
			return
				GetGenericInterfaceImplementation(method.DeclaringType, genericInterfaceType)
				.Any(constructedInterfaceType => IsImplementationOfInterfaceMethod(method, constructedInterfaceType, methodName));
		}

		static bool IsImplementationOfInterfaceMethod(MethodInfo method, Type interfaceType, string methodName) {
			Console.WriteLine(method.DeclaringType);
			if (!interfaceType.IsAssignableFrom(method.DeclaringType)) return false;
			Console.WriteLine(method.DeclaringType);
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
