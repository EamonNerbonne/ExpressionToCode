using System;
using System.Linq.Expressions;
using System.Diagnostics.Contracts;

namespace ExpressionToCodeLib {
  public interface IExpressionTypeDispatch<T> {
		[Pure] T DispatchAdd(Expression e);
		[Pure] T DispatchAddChecked(Expression e);
		[Pure] T DispatchAnd(Expression e);
		[Pure] T DispatchAndAlso(Expression e);
		[Pure] T DispatchArrayLength(Expression e);
		[Pure] T DispatchArrayIndex(Expression e);
		[Pure] T DispatchCall(Expression e);
		[Pure] T DispatchCoalesce(Expression e);
		[Pure] T DispatchConditional(Expression e);
		[Pure] T DispatchConstant(Expression e);
		[Pure] T DispatchConvert(Expression e);
		[Pure] T DispatchConvertChecked(Expression e);
		[Pure] T DispatchDivide(Expression e);
		[Pure] T DispatchEqual(Expression e);
		[Pure] T DispatchExclusiveOr(Expression e);
		[Pure] T DispatchGreaterThan(Expression e);
		[Pure] T DispatchGreaterThanOrEqual(Expression e);
		[Pure] T DispatchInvoke(Expression e);
		[Pure] T DispatchLambda(Expression e);
		[Pure] T DispatchLeftShift(Expression e);
		[Pure] T DispatchLessThan(Expression e);
		[Pure] T DispatchLessThanOrEqual(Expression e);
		[Pure] T DispatchListInit(Expression e);
		[Pure] T DispatchMemberAccess(Expression e);
		[Pure] T DispatchMemberInit(Expression e);
		[Pure] T DispatchModulo(Expression e);
		[Pure] T DispatchMultiply(Expression e);
		[Pure] T DispatchMultiplyChecked(Expression e);
		[Pure] T DispatchNegate(Expression e);
		[Pure] T DispatchUnaryPlus(Expression e);
		[Pure] T DispatchNegateChecked(Expression e);
		[Pure] T DispatchNew(Expression e);
		[Pure] T DispatchNewArrayInit(Expression e);
		[Pure] T DispatchNewArrayBounds(Expression e);
		[Pure] T DispatchNot(Expression e);
		[Pure] T DispatchNotEqual(Expression e);
		[Pure] T DispatchOr(Expression e);
		[Pure] T DispatchOrElse(Expression e);
		[Pure] T DispatchParameter(Expression e);
		[Pure] T DispatchPower(Expression e);
		[Pure] T DispatchQuote(Expression e);
		[Pure] T DispatchRightShift(Expression e);
		[Pure] T DispatchSubtract(Expression e);
		[Pure] T DispatchSubtractChecked(Expression e);
		[Pure] T DispatchTypeAs(Expression e);
		[Pure] T DispatchTypeIs(Expression e);
		[Pure] T DispatchAssign(Expression e);
		[Pure] T DispatchBlock(Expression e);
		[Pure] T DispatchDebugInfo(Expression e);
		[Pure] T DispatchDecrement(Expression e);
		[Pure] T DispatchDynamic(Expression e);
		[Pure] T DispatchDefault(Expression e);
		[Pure] T DispatchExtension(Expression e);
		[Pure] T DispatchGoto(Expression e);
		[Pure] T DispatchIncrement(Expression e);
		[Pure] T DispatchIndex(Expression e);
		[Pure] T DispatchLabel(Expression e);
		[Pure] T DispatchRuntimeVariables(Expression e);
		[Pure] T DispatchLoop(Expression e);
		[Pure] T DispatchSwitch(Expression e);
		[Pure] T DispatchThrow(Expression e);
		[Pure] T DispatchTry(Expression e);
		[Pure] T DispatchUnbox(Expression e);
		[Pure] T DispatchAddAssign(Expression e);
		[Pure] T DispatchAndAssign(Expression e);
		[Pure] T DispatchDivideAssign(Expression e);
		[Pure] T DispatchExclusiveOrAssign(Expression e);
		[Pure] T DispatchLeftShiftAssign(Expression e);
		[Pure] T DispatchModuloAssign(Expression e);
		[Pure] T DispatchMultiplyAssign(Expression e);
		[Pure] T DispatchOrAssign(Expression e);
		[Pure] T DispatchPowerAssign(Expression e);
		[Pure] T DispatchRightShiftAssign(Expression e);
		[Pure] T DispatchSubtractAssign(Expression e);
		[Pure] T DispatchAddAssignChecked(Expression e);
		[Pure] T DispatchMultiplyAssignChecked(Expression e);
		[Pure] T DispatchSubtractAssignChecked(Expression e);
		[Pure] T DispatchPreIncrementAssign(Expression e);
		[Pure] T DispatchPreDecrementAssign(Expression e);
		[Pure] T DispatchPostIncrementAssign(Expression e);
		[Pure] T DispatchPostDecrementAssign(Expression e);
		[Pure] T DispatchTypeEqual(Expression e);
		[Pure] T DispatchOnesComplement(Expression e);
		[Pure] T DispatchIsTrue(Expression e);
		[Pure] T DispatchIsFalse(Expression e);
  }

    public static class ExpressionTypeDispatcher {
		[Pure] public static T ExpressionDispatch<T>(this IExpressionTypeDispatch<T> dispatcher, Expression e) {
			try{
				switch(e.NodeType) {
					case ExpressionType.Add: return dispatcher.DispatchAdd(e);
					case ExpressionType.AddChecked: return dispatcher.DispatchAddChecked(e);
					case ExpressionType.And: return dispatcher.DispatchAnd(e);
					case ExpressionType.AndAlso: return dispatcher.DispatchAndAlso(e);
					case ExpressionType.ArrayLength: return dispatcher.DispatchArrayLength(e);
					case ExpressionType.ArrayIndex: return dispatcher.DispatchArrayIndex(e);
					case ExpressionType.Call: return dispatcher.DispatchCall(e);
					case ExpressionType.Coalesce: return dispatcher.DispatchCoalesce(e);
					case ExpressionType.Conditional: return dispatcher.DispatchConditional(e);
					case ExpressionType.Constant: return dispatcher.DispatchConstant(e);
					case ExpressionType.Convert: return dispatcher.DispatchConvert(e);
					case ExpressionType.ConvertChecked: return dispatcher.DispatchConvertChecked(e);
					case ExpressionType.Divide: return dispatcher.DispatchDivide(e);
					case ExpressionType.Equal: return dispatcher.DispatchEqual(e);
					case ExpressionType.ExclusiveOr: return dispatcher.DispatchExclusiveOr(e);
					case ExpressionType.GreaterThan: return dispatcher.DispatchGreaterThan(e);
					case ExpressionType.GreaterThanOrEqual: return dispatcher.DispatchGreaterThanOrEqual(e);
					case ExpressionType.Invoke: return dispatcher.DispatchInvoke(e);
					case ExpressionType.Lambda: return dispatcher.DispatchLambda(e);
					case ExpressionType.LeftShift: return dispatcher.DispatchLeftShift(e);
					case ExpressionType.LessThan: return dispatcher.DispatchLessThan(e);
					case ExpressionType.LessThanOrEqual: return dispatcher.DispatchLessThanOrEqual(e);
					case ExpressionType.ListInit: return dispatcher.DispatchListInit(e);
					case ExpressionType.MemberAccess: return dispatcher.DispatchMemberAccess(e);
					case ExpressionType.MemberInit: return dispatcher.DispatchMemberInit(e);
					case ExpressionType.Modulo: return dispatcher.DispatchModulo(e);
					case ExpressionType.Multiply: return dispatcher.DispatchMultiply(e);
					case ExpressionType.MultiplyChecked: return dispatcher.DispatchMultiplyChecked(e);
					case ExpressionType.Negate: return dispatcher.DispatchNegate(e);
					case ExpressionType.UnaryPlus: return dispatcher.DispatchUnaryPlus(e);
					case ExpressionType.NegateChecked: return dispatcher.DispatchNegateChecked(e);
					case ExpressionType.New: return dispatcher.DispatchNew(e);
					case ExpressionType.NewArrayInit: return dispatcher.DispatchNewArrayInit(e);
					case ExpressionType.NewArrayBounds: return dispatcher.DispatchNewArrayBounds(e);
					case ExpressionType.Not: return dispatcher.DispatchNot(e);
					case ExpressionType.NotEqual: return dispatcher.DispatchNotEqual(e);
					case ExpressionType.Or: return dispatcher.DispatchOr(e);
					case ExpressionType.OrElse: return dispatcher.DispatchOrElse(e);
					case ExpressionType.Parameter: return dispatcher.DispatchParameter(e);
					case ExpressionType.Power: return dispatcher.DispatchPower(e);
					case ExpressionType.Quote: return dispatcher.DispatchQuote(e);
					case ExpressionType.RightShift: return dispatcher.DispatchRightShift(e);
					case ExpressionType.Subtract: return dispatcher.DispatchSubtract(e);
					case ExpressionType.SubtractChecked: return dispatcher.DispatchSubtractChecked(e);
					case ExpressionType.TypeAs: return dispatcher.DispatchTypeAs(e);
					case ExpressionType.TypeIs: return dispatcher.DispatchTypeIs(e);
					case ExpressionType.Assign: return dispatcher.DispatchAssign(e);
					case ExpressionType.Block: return dispatcher.DispatchBlock(e);
					case ExpressionType.DebugInfo: return dispatcher.DispatchDebugInfo(e);
					case ExpressionType.Decrement: return dispatcher.DispatchDecrement(e);
					case ExpressionType.Dynamic: return dispatcher.DispatchDynamic(e);
					case ExpressionType.Default: return dispatcher.DispatchDefault(e);
					case ExpressionType.Extension: return dispatcher.DispatchExtension(e);
					case ExpressionType.Goto: return dispatcher.DispatchGoto(e);
					case ExpressionType.Increment: return dispatcher.DispatchIncrement(e);
					case ExpressionType.Index: return dispatcher.DispatchIndex(e);
					case ExpressionType.Label: return dispatcher.DispatchLabel(e);
					case ExpressionType.RuntimeVariables: return dispatcher.DispatchRuntimeVariables(e);
					case ExpressionType.Loop: return dispatcher.DispatchLoop(e);
					case ExpressionType.Switch: return dispatcher.DispatchSwitch(e);
					case ExpressionType.Throw: return dispatcher.DispatchThrow(e);
					case ExpressionType.Try: return dispatcher.DispatchTry(e);
					case ExpressionType.Unbox: return dispatcher.DispatchUnbox(e);
					case ExpressionType.AddAssign: return dispatcher.DispatchAddAssign(e);
					case ExpressionType.AndAssign: return dispatcher.DispatchAndAssign(e);
					case ExpressionType.DivideAssign: return dispatcher.DispatchDivideAssign(e);
					case ExpressionType.ExclusiveOrAssign: return dispatcher.DispatchExclusiveOrAssign(e);
					case ExpressionType.LeftShiftAssign: return dispatcher.DispatchLeftShiftAssign(e);
					case ExpressionType.ModuloAssign: return dispatcher.DispatchModuloAssign(e);
					case ExpressionType.MultiplyAssign: return dispatcher.DispatchMultiplyAssign(e);
					case ExpressionType.OrAssign: return dispatcher.DispatchOrAssign(e);
					case ExpressionType.PowerAssign: return dispatcher.DispatchPowerAssign(e);
					case ExpressionType.RightShiftAssign: return dispatcher.DispatchRightShiftAssign(e);
					case ExpressionType.SubtractAssign: return dispatcher.DispatchSubtractAssign(e);
					case ExpressionType.AddAssignChecked: return dispatcher.DispatchAddAssignChecked(e);
					case ExpressionType.MultiplyAssignChecked: return dispatcher.DispatchMultiplyAssignChecked(e);
					case ExpressionType.SubtractAssignChecked: return dispatcher.DispatchSubtractAssignChecked(e);
					case ExpressionType.PreIncrementAssign: return dispatcher.DispatchPreIncrementAssign(e);
					case ExpressionType.PreDecrementAssign: return dispatcher.DispatchPreDecrementAssign(e);
					case ExpressionType.PostIncrementAssign: return dispatcher.DispatchPostIncrementAssign(e);
					case ExpressionType.PostDecrementAssign: return dispatcher.DispatchPostDecrementAssign(e);
					case ExpressionType.TypeEqual: return dispatcher.DispatchTypeEqual(e);
					case ExpressionType.OnesComplement: return dispatcher.DispatchOnesComplement(e);
					case ExpressionType.IsTrue: return dispatcher.DispatchIsTrue(e);
					case ExpressionType.IsFalse: return dispatcher.DispatchIsFalse(e);
					default:
						if(Enum.IsDefined(typeof(ExpressionType),e.NodeType))
							throw new NotImplementedException("ExpressionToCode supports .NET 3.5 expressions only");
						else
							throw new ArgumentOutOfRangeException("Impossible enum value:"+(int)e.NodeType);
				}
			} catch(NotImplementedException nie) {
				throw new ArgumentOutOfRangeException("Could not dispatch expr with nodetype "+e.NodeType+" and type " +e.GetType().Name,nie);
			}
		}
	}
}
 
