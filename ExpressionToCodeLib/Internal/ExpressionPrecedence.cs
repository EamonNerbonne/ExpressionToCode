using System;
using System.Linq.Expressions;

namespace ExpressionToCodeLib.Internal
{
    static class ExpressionPrecedence
    {
        static bool UnaryDashSym(ExpressionType et)
            => et == ExpressionType.Negate
                || et == ExpressionType.NegateChecked
                || et == ExpressionType.PreDecrementAssign;

        static bool UnaryPlusSym(ExpressionType et)
            => et == ExpressionType.UnaryPlus
                || et == ExpressionType.PreIncrementAssign;

        public static bool TokenizerConfusable(ExpressionType a, ExpressionType b)
            => UnaryDashSym(a) && UnaryDashSym(b) || UnaryPlusSym(a) && UnaryPlusSym(b);

        public static int Rank(ExpressionType exprType)
            => exprType switch {
                //brackets make no sense:
                ExpressionType.Block => -1,
                ExpressionType.Goto => -1,
                ExpressionType.Loop => -1,
                ExpressionType.Switch => -1,
                ExpressionType.Throw => -1,
                ExpressionType.Try => -1,
                ExpressionType.Label => -1,
                //brackets built-in; thus unnecesary (for params only!).
                ExpressionType.MemberInit => 1,
                ExpressionType.ArrayIndex => 1,
                ExpressionType.Call => 1,
                ExpressionType.Invoke => 1,
                ExpressionType.New => 1,
                ExpressionType.NewArrayInit => 1,
                ExpressionType.NewArrayBounds => 1,
                ExpressionType.ListInit => 1,
                ExpressionType.Power => 1 //non-native, uses Call.
                ,
                //other primary expressions
                ExpressionType.Constant => 1,
                ExpressionType.Parameter => 1,
                ExpressionType.MemberAccess => 1,
                ExpressionType.ArrayLength => 1,
                ExpressionType.Index => 1,
                ExpressionType.Default => 1,
                ExpressionType.PostIncrementAssign => 1,
                ExpressionType.PostDecrementAssign => 1,
                //unary prefixes
                ExpressionType.UnaryPlus => 2,
                ExpressionType.Negate => 2,
                ExpressionType.NegateChecked => 2,
                ExpressionType.Convert => 2,
                ExpressionType.ConvertChecked => 2,
                ExpressionType.Not => 2 //bitwise OR numeric!
                ,
                ExpressionType.OnesComplement => 2 //numeric
                ,
                ExpressionType.IsTrue => 2 //maybe?
                ,
                ExpressionType.IsFalse => 2 //maybe?
                ,
                ExpressionType.PreIncrementAssign => 2,
                ExpressionType.PreDecrementAssign => 2,
                //binary multiplicative
                ExpressionType.Modulo => 3,
                ExpressionType.Multiply => 3,
                ExpressionType.MultiplyChecked => 3,
                ExpressionType.Divide => 3,
                //binary addition
                ExpressionType.Add => 4,
                ExpressionType.AddChecked => 4,
                ExpressionType.Subtract => 4,
                ExpressionType.SubtractChecked => 4,
                ExpressionType.Decrement => 4 //nonnative; uses ... - 1
                ,
                ExpressionType.Increment => 4 //nonnative; uses ... - 1
                ,
                //binary shift
                ExpressionType.LeftShift => 5,
                ExpressionType.RightShift => 5,
                //relational excl. equals
                ExpressionType.LessThan => 6,
                ExpressionType.LessThanOrEqual => 6,
                ExpressionType.GreaterThan => 6,
                ExpressionType.GreaterThanOrEqual => 6,
                ExpressionType.TypeAs => 6,
                ExpressionType.TypeIs => 6,
                //equality
                ExpressionType.NotEqual => 7,
                ExpressionType.Equal => 7,
                //bitwise/eager
                ExpressionType.And => 8,
                ExpressionType.ExclusiveOr => 9,
                ExpressionType.Or => 10,
                //logical/shortcircuit:
                ExpressionType.AndAlso => 11,
                ExpressionType.OrElse => 12,
                //null-coalesce
                ExpressionType.Coalesce => 13,
                //ternary ? :
                ExpressionType.Conditional => 14,
                //assignments & lamba's
                ExpressionType.Lambda => 15,
                ExpressionType.Quote => 15 //maybe?
                ,
                ExpressionType.Assign => 15,
                ExpressionType.AddAssign => 15,
                ExpressionType.AndAssign => 15,
                ExpressionType.DivideAssign => 15,
                ExpressionType.ExclusiveOrAssign => 15,
                ExpressionType.LeftShiftAssign => 15,
                ExpressionType.ModuloAssign => 15,
                ExpressionType.MultiplyAssign => 15,
                ExpressionType.OrAssign => 15,
                ExpressionType.PowerAssign => 15,
                ExpressionType.RightShiftAssign => 15,
                ExpressionType.SubtractAssign => 15,
                ExpressionType.AddAssignChecked => 15,
                ExpressionType.MultiplyAssignChecked => 15,
                ExpressionType.SubtractAssignChecked => 15,
                _ => throw new ArgumentOutOfRangeException("Unsupported enum value:" + exprType)
            };
    }
}
