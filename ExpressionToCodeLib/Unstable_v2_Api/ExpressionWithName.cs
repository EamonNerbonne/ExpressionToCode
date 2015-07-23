using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace ExpressionToCodeLib.Unstable_v2_Api
{
	public static class ExpressionWithName
	{
		/// <summary>
		/// Gets property, variable or method name from lambda expression.
		/// </summary>
		/// <param name="expression"></param>
		/// <returns></returns>
		/// <example>
		/// var example = "some text";
		/// var name = toName( () => example);  // "example"
		/// </example>
		public static string ToNameOf<TResult>(this Expression<Func<TResult>> expression) => ToNameOfInternal(expression);

		public static string ToNameOf<T1, T2, TResult>(this Expression<Func<T1, T2, TResult>> expression) => ToNameOfInternal(expression);

		public static string ToNameOf<T1, TResult>(this Expression<Func<T1, TResult>> expression) => ToNameOfInternal(expression);

		public static string ToNameOf<T1, T2,T3, TResult>(this Expression<Func<T1, T2,T3, TResult>> expression) => ToNameOfInternal(expression);
		
		public static string ToNameOf<T>(this Expression<Action<T>> expression) => ToNameOfInternal(expression);

		public static string ToNameOf<T1,T2>(this Expression<Action<T1,T2>> expression) => ToNameOfInternal(expression);

		public static string ToNameOf(this Expression<Action> expression) => ToNameOfInternal(expression);

		public static string ToNameOf<T>(this Expression<T> expression) => ToNameOfInternal(expression);

		private static string ToNameOfInternal<T>(Expression<T> expression)
		{
			string value = null;

			var unaryExpression = expression.Body as UnaryExpression;
			if (unaryExpression != null)
				value = unaryExpression.Operand.ToString().Split('.').Last();

			var memberExpression = expression.Body as MemberExpression;
			if (memberExpression != null)
				value = memberExpression.Member.Name;

			var methodCallExpression = expression.Body as MethodCallExpression;
			if (methodCallExpression != null)
				value = methodCallExpression.Method.Name;

			if (value == null)
				throw new ArgumentException("expression", "Unsupported or unknown or complex expression to get `name` of it");
			return value;
		}

		//NOTE: should use recursive visitor as in other method when new failed test case added
		public static string ToFullNameOf<T>(this Expression<T> expression)
		{
			string name = null;
			var unaryExpression = expression.Body as UnaryExpression;
			if(unaryExpression != null) {
				name = unaryExpression.Operand.ToString().Split('.').Last();
				if(unaryExpression.NodeType == ExpressionType.ArrayLength) {
					name += ".Length";
				}
			}
			var memberExpression = expression.Body as MemberExpression;
			if(memberExpression != null) {
				name = memberExpression.Member.Name;
			}
			var methodCallExpression = expression.Body as MethodCallExpression;
			if(methodCallExpression != null) {
				// tries transform method and return value in human readable C#-style representation

				// add declaring type if it is not a module
				var arguments = string.Join(
					", ",
					methodCallExpression.Arguments.Select(x => x.ToString()).ToArray() // converting to string to work for .NET 3.5 if backported
                    //methodCallExpression.Method.GetParameters().Select(x => x.Name).ToArray() // converting to string to work for .NET 3.5 if backported
				);
				var method = methodCallExpression.Method;
				var methodName = method.Name;
				if(method.IsGenericMethod) {
					methodName += "<" + String.Join(
						", ",
						method.GetGenericArguments().Select(x => x.Name).ToArray()) // converting to string to work for .NET 3.5 if backported
						+ ">";
				}
				if(methodName == "get_Item" && methodCallExpression.Arguments.Count > 0) {
					//indexed property
					string typeName = methodCallExpression.Object != null ? methodCallExpression.Object.Type.Name : "";
					name = typeName + "[" + arguments + "]";
				} else {
					var typePrefix = "";
					if(method.IsStatic) {
						typePrefix = method.DeclaringType.Name + ".";
					}
					name = typePrefix + methodName + "(" + arguments + ")";
				}
			}
			if(name == null) {
				throw new ArgumentException("Failed to translate expression to its valued representation", "expression");
			}
			return name;
		}
	}
}
