using System;
using System.Linq.Expressions;

namespace ExpressionToCodeLib
{
    struct StringifiedExpression
    {
        //a node cannot have children and text.  If it has neither, it is considered empty.
        public readonly string Text;
        readonly StringifiedExpression[] children;
        static readonly StringifiedExpression[] empty = new StringifiedExpression[0];
        public StringifiedExpression[] Children => children ?? empty;
        //can only have a value it it has text.
        public readonly Expression OptionalValue;

        StringifiedExpression(string text, StringifiedExpression[] children, Expression optionalValue)
        {
            Text = text;
            this.children = children;
            OptionalValue = optionalValue;
        }

        public static StringifiedExpression TextOnly(string text) => new StringifiedExpression(text, null, null);

        public static StringifiedExpression TextAndExpr(string text, Expression expr)
        {
            if (expr == null) {
                throw new ArgumentNullException(nameof(expr));
            }
            return new StringifiedExpression(text, null, expr);
        }

        public static StringifiedExpression WithChildren(StringifiedExpression[] children) => new StringifiedExpression(null, children, null);
        public override string ToString() => Text ?? string.Join("", children);
    }
}
