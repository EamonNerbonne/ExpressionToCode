using System;
using System.Linq.Expressions;

namespace ExpressionToCodeLib
{
    struct StringifiedExpression
    {
        public readonly int Depth;
        //a node cannot have children and text.  If it has neither, it is considered empty.
        public readonly string Text;
        readonly StringifiedExpression[] children;
        static readonly StringifiedExpression[] empty = new StringifiedExpression[0];
        public StringifiedExpression[] Children => children ?? empty;
        //can only have a value it it has text.
        public readonly Expression OptionalValue;

        StringifiedExpression(int depth, string text, StringifiedExpression[] children, Expression optionalValue)
        {
            Text = text;
            this.children = children;
            OptionalValue = optionalValue;
            Depth = depth;
        }

        public static StringifiedExpression TextOnly(string text, int depth) => new StringifiedExpression(depth, text, null, null);

        public static StringifiedExpression TextAndExpr(string text, Expression expr, int depth)
        {
            if (expr == null) {
                throw new ArgumentNullException(nameof(expr));
            }
            return new StringifiedExpression(depth, text, null, expr);
        }

        public static StringifiedExpression WithChildren(StringifiedExpression[] children, int depth) => new StringifiedExpression(depth, null, children, null);
    }

}
