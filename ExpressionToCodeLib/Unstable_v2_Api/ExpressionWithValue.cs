using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using ExpressionToCodeLib;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace ExpressionToCodeTest.Unstable_v2_Api
{
    public static class ExpressionWithValue
    {
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
        public static string ToValuedCode<TResult>(this Expression<Func<TResult>> expression)
        {
            TResult retValue;
            try {
                retValue = expression.Compile().Invoke();
            } catch(Exception ex) {
                throw new InvalidOperationException("Cannon get return value of expression when it throws error", ex);
            }
            return ExpressionToCodeLib.Unstable_v2_Api.ExpressionWithName.ToFullNameOf(expression) + " = " + retValue;
        }
    }
}
