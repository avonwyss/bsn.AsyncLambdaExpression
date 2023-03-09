using System;
using System.Linq.Expressions;

namespace bsn.AsyncLambdaExpression.Expressions {
	internal partial class ContinuationBuilder {
		protected override Expression VisitMethodCall(MethodCallExpression node) {
			if (AsyncExpressionExtensions.IsAwaitMethod(node.Method)) {
				var nextState = this.CreateState(node.Type);
				nextState.SetName("Await", this.currentState.StateId, "GetResult");
				this.currentState.SetContinuation(nextState);
				var exprAwaitable = this.Visit(node.Arguments[0]);
				var varAwaiter = this.vars.GetVarAwaiter(exprAwaitable.Type.GetAwaitableGetAwaiterMethod().ReturnType);
				this.currentState.AddExpression(
						Expression.IfThen(
								Expression.Not(
										Expression.Property(
												Expression.Assign(
														varAwaiter,
														Expression.Call(exprAwaitable, exprAwaitable.Type.GetAwaitableGetAwaiterMethod())),
												varAwaiter.Type.GetAwaiterIsCompletedProperty())),
								Expression.Block(
										Expression.Call(
												varAwaiter,
												varAwaiter.Type.GetAwaiterOnCompletedMethod(), this.vars.VarContinuation),
										Expression.Break(this.vars.LblBreak, Expression.Default(this.vars.LblBreak.Type)))));
				nextState.AddExpression(varAwaiter.Type.GetAwaiterGetResultMethod().ReturnType == typeof(void)
						? Expression.Call(varAwaiter, varAwaiter.Type.GetAwaiterGetResultMethod())
						: Expression.Assign(nextState.ResultExpression, Expression.Call(varAwaiter, varAwaiter.Type.GetAwaiterGetResultMethod())));
				this.currentState = nextState;
				return this.currentState.ResultExpression;
			}
			if (IteratorExpressionExtensions.IsYieldReturnMethod(node.Method)) {
				var nextState = this.CreateState(typeof(void));
				nextState.SetName("Yield", this.currentState.StateId, "Return");
				this.currentState.SetContinuation(nextState);
				var exprResult = this.Visit(node.Arguments[0]);
				this.currentState.AddExpression(
						Expression.Assign(
								Expression.Field(this.vars.VarCurrent, this.vars.VarCurrent.Type.GetStrongBoxValueField()),
								exprResult));
				this.currentState.AddExpression(
						Expression.Break(this.vars.LblBreak,
								Expression.Constant(true)));
				this.currentState = nextState;
				return this.currentState.ResultExpression;
			}
			return base.VisitMethodCall(node);
		}
	}
}
