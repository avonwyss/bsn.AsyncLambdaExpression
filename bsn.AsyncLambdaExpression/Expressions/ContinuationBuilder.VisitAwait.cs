using System;
using System.Linq.Expressions;

namespace bsn.AsyncLambdaExpression.Expressions {
	internal partial class ContinuationBuilder {
		public Expression VisitAwait(AwaitExpression node) {
			var nextState = this.CreateState(node.Type);
			nextState.SetName("Await", this.currentState.StateId, "GetResult");
			this.currentState.SetContinuation(nextState);
			var exprAwaitable = this.Visit(node.Awaitable);
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
	}
}
