using System;
using System.Linq.Expressions;

namespace ExpressionToCodeLib {
	static class ExpressionPrecedence {
		static bool UnaryDashSym(ExpressionType et) {
			return et == ExpressionType.Negate
				|| et == ExpressionType.NegateChecked
#if DOTNET40
				|| et == ExpressionType.PreDecrementAssign
#endif
				;
		}
		static bool UnaryPlusSym(ExpressionType et) {
			return et == ExpressionType.UnaryPlus
#if DOTNET40
				|| et == ExpressionType.PreIncrementAssign
#endif
				;
		}
		public static bool TokenizerConfusable(ExpressionType a, ExpressionType b) {
			return UnaryDashSym(a) && UnaryDashSym(b) || UnaryPlusSym(a) && UnaryPlusSym(b);
		}

		public static int Rank(ExpressionType exprType) {
			switch (exprType) {
#if DOTNET40
				//brackets make no sense:
				case ExpressionType.Block: return -1;
				case ExpressionType.Goto: return -1;
				case ExpressionType.Loop: return -1;
				case ExpressionType.Switch: return -1;
				case ExpressionType.Throw: return -1;
				case ExpressionType.Try: return -1;
				case ExpressionType.Label: return -1;
#endif

				//brackets built-in; thus unnecesary (for params only!).
				case ExpressionType.MemberInit: return 1;
				case ExpressionType.ArrayIndex: return 1;
				case ExpressionType.Call: return 1;
				case ExpressionType.Invoke: return 1;
				case ExpressionType.New: return 1;
				case ExpressionType.NewArrayInit: return 1;
				case ExpressionType.NewArrayBounds: return 1;
				case ExpressionType.ListInit: return 1;
				case ExpressionType.Power: return 1;//non-native, uses Call.

				//other primary expressions
				case ExpressionType.Constant: return 1;
				case ExpressionType.Parameter: return 1;
				case ExpressionType.MemberAccess: return 1;
				case ExpressionType.ArrayLength: return 1;
#if DOTNET40
				case ExpressionType.Index: return 1;
				case ExpressionType.Default: return 1;
				case ExpressionType.PostIncrementAssign: return 1;
				case ExpressionType.PostDecrementAssign: return 1;
#endif

				//unary prefixes
				case ExpressionType.UnaryPlus: return 2;
				case ExpressionType.Negate: return 2;
				case ExpressionType.NegateChecked: return 2;
				case ExpressionType.Convert: return 2;
				case ExpressionType.ConvertChecked: return 2;
				case ExpressionType.Not: return 2;//bitwise OR numeric!
#if DOTNET40
				case ExpressionType.OnesComplement: return 2; //numeric
				case ExpressionType.IsTrue: return 2;//maybe?
				case ExpressionType.IsFalse: return 2;//maybe?
				case ExpressionType.PreIncrementAssign: return 2;
				case ExpressionType.PreDecrementAssign: return 2;
#endif

				//binary multiplicative
				case ExpressionType.Modulo: return 3;
				case ExpressionType.Multiply: return 3;
				case ExpressionType.MultiplyChecked: return 3;
				case ExpressionType.Divide: return 3;

				//binary addition
				case ExpressionType.Add: return 4;
				case ExpressionType.AddChecked: return 4;
				case ExpressionType.Subtract: return 4;
				case ExpressionType.SubtractChecked: return 4;
#if DOTNET40
				case ExpressionType.Decrement: return 4;//nonnative; uses ... - 1
				case ExpressionType.Increment: return 4;//nonnative; uses ... - 1
#endif

				//binary shift
				case ExpressionType.LeftShift: return 5;
				case ExpressionType.RightShift: return 5;

				//relational excl. equals
				case ExpressionType.LessThan: return 6;
				case ExpressionType.LessThanOrEqual: return 6;
				case ExpressionType.GreaterThan: return 6;
				case ExpressionType.GreaterThanOrEqual: return 6;
				case ExpressionType.TypeAs: return 6;
				case ExpressionType.TypeIs: return 6;

				//equality
				case ExpressionType.NotEqual: return 7;
				case ExpressionType.Equal: return 7;

				//bitwise/eager
				case ExpressionType.And: return 8;
				case ExpressionType.ExclusiveOr: return 9;
				case ExpressionType.Or: return 10;

				//logical/shortcircuit:
				case ExpressionType.AndAlso: return 11;
				case ExpressionType.OrElse: return 12;

				//null-coalesce
				case ExpressionType.Coalesce: return 13;

				//ternary ? : 
				case ExpressionType.Conditional: return 14;

				//assignments & lamba's
				case ExpressionType.Lambda: return 15;
				case ExpressionType.Quote: return 15;//maybe?
#if DOTNET40
				case ExpressionType.Assign: return 15;
				case ExpressionType.AddAssign: return 15;
				case ExpressionType.AndAssign: return 15;
				case ExpressionType.DivideAssign: return 15;
				case ExpressionType.ExclusiveOrAssign: return 15;
				case ExpressionType.LeftShiftAssign: return 15;
				case ExpressionType.ModuloAssign: return 15;
				case ExpressionType.MultiplyAssign: return 15;
				case ExpressionType.OrAssign: return 15;
				case ExpressionType.PowerAssign: return 15;
				case ExpressionType.RightShiftAssign: return 15;
				case ExpressionType.SubtractAssign: return 15;
				case ExpressionType.AddAssignChecked: return 15;
				case ExpressionType.MultiplyAssignChecked: return 15;
				case ExpressionType.SubtractAssignChecked: return 15;
#endif

				//Can't deal with these:
				/*
			case ExpressionType.Dynamic: return 0;//hmm...
			case ExpressionType.Extension: return 0;
			case ExpressionType.DebugInfo: return 0;//hmm...
			case ExpressionType.RuntimeVariables: return 0;//hmm...
			case ExpressionType.TypeEqual: return 0;//hmm...
			case ExpressionType.Unbox: return 0;//hmm...
				*/

				default: throw new ArgumentOutOfRangeException("Unsupported enum value:" + exprType);
			}
		}
	}
}
