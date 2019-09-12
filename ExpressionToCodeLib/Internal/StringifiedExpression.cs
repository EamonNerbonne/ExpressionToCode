using System;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;

namespace ExpressionToCodeLib.Internal
{
    struct StringifiedExpression
    {
        //a node cannot have children and text.  If it has neither, it is considered empty.
        public readonly string? Text;
        readonly StringifiedExpression[]? children;

        //can only have a value it it has text.
        public readonly Expression? OptionalValue;

        /// <summary>
        ///     The expression tree contains many symbols that are not themselves "real" expressions, e.g. the "." in "obj.field".
        ///     This field is true for parts that aren't just implemenation details, but proper sub-expressions; e.g. the "x" in "x
        ///     &amp;&amp; y"
        /// </summary>
        public readonly bool IsConceptualChild;

        static readonly StringifiedExpression[] empty = { };
        public StringifiedExpression[] Children => children ?? empty;

        StringifiedExpression(string? text, StringifiedExpression[]? children, Expression? optionalValue, bool isConceptualChild)
        {
            Text = text;
            this.children = children;
            OptionalValue = optionalValue;
            IsConceptualChild = isConceptualChild;
        }

        [Pure]
        public static StringifiedExpression TextOnly(string? text)
            => new StringifiedExpression(text, null, null, false);

        [Pure]
        public static StringifiedExpression TextAndExpr(string text, Expression expr)
        {
            if (expr == null) {
                throw new ArgumentNullException(nameof(expr));
            }

            return new StringifiedExpression(text, null, expr, false);
        }

        [Pure]
        public static StringifiedExpression WithChildren(StringifiedExpression[] children)
            => new StringifiedExpression(null, children, null, false);

        [Pure]
        public override string ToString()
            => Text ?? string.Join("", Children);

        public StringifiedExpression MarkAsConceptualChild()
            => new StringifiedExpression(Text, children, OptionalValue, true);
    }
}
