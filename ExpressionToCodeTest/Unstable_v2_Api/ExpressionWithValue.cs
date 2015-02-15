using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using ExpressionToCodeLib;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace ExpressionToCodeTest.Unstable_v2_Api {
    public static class ExpressionWithValue {
        ///<summary>
        /// Converts expression to variable/property/method C# like representation adding it's string value.
        ///</summary>
        /// <example>
        /// string toNameValueRepresentation = "Value";
        /// ToRepr(() => toNameValueRepresentation); // "toNameValueRepresentation = Value"
        /// </example>
        /// <remarks>
        /// Unlike <see cref="ExpressionToCode.ToCode"/>(which targets compilable output), this method is geared towards dumping simple objects into text, so may skip some C# issues for sake of readability.
        /// </remarks>
        public static string ToValuedCode<TResult>(this Expression<Func<TResult>> expression) {
            TResult retValue;
            try {
                retValue = expression.Compile().Invoke();
            } catch (Exception ex) {
                throw new InvalidOperationException("Cannon get return value of expression when it throws error", ex);
            }

            var name = ToFullName(expression);

            return name + " = " + retValue;
        }

        //NOTE: should use recursive visitor as in other method when new failed test case added
        static string ToFullName<T>(this Expression<T> expression) {
            string name = null;
            var unaryExpression = expression.Body as UnaryExpression;
            if (unaryExpression != null) {
                name = unaryExpression.Operand.ToString().Split('.').Last();
                if (unaryExpression.NodeType == ExpressionType.ArrayLength) {
                    name += ".Length";
                }
            }
            var memberExpression = expression.Body as MemberExpression;
            if (memberExpression != null) {
                name = memberExpression.Member.Name;
            }
            var methodCallExpression = expression.Body as MethodCallExpression;
            if (methodCallExpression != null) {
                // tries transform method and return value in human readable C#-style representation

                // add declaring type if it is not a module
                var arguments = string.Join(
                    ", ",
                    methodCallExpression.Arguments.Select(x => x.ToString()).ToArray() // converting to string to work for .NET 3.5 if backported
                    );
                var method = methodCallExpression.Method;
                var methodName = method.Name;
                if (method.IsGenericMethod) {
                    methodName += "<" + String.Join(
                        ", ",
                        method.GetGenericArguments().Select(x => x.Name).ToArray()) // converting to string to work for .NET 3.5 if backported
                        + ">";
                }
                if (methodName == "get_Item" && methodCallExpression.Arguments.Count > 0) {
                    //indexed property
                    string typeName = methodCallExpression.Object != null ? methodCallExpression.Object.Type.Name : "";
                    name = typeName + "[" + arguments + "]";
                } else {
                    var typePrefix = "";
                    if (method.IsStatic) {
                        typePrefix = method.DeclaringType.Name + ".";
                    }
                    name = typePrefix + methodName + "(" + arguments + ")";
                }
            }
            if (name == null) {
                throw new ArgumentException("Failed to translate expression to its valued representation", "expression");
            }
            return name;
        }
    }
}
