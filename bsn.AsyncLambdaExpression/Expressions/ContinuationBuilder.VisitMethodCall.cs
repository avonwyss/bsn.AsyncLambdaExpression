using System.Linq.Expressions;

namespace bsn.AsyncLambdaExpression.Expressions {
	internal partial class ContinuationBuilder {
		protected override Expression VisitMethodCall(MethodCallExpression node) {
			if (AsyncStateMachineBuilder.IsAwaitExpression(node)) {
				var nextState = CreateState(node.Type);
				currentState.SetContinuation(nextState);
				var exprAwaitable = Visit(node.Arguments[0]);
				var varAwaiter = builder.GetVarAwaiter(exprAwaitable.Type.GetAwaitableGetAwaiterMethod().ReturnType);
				currentState.AddExpression(
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
												varAwaiter.Type.GetAwaiterOnCompletedMethod(),
												builder.VarContinuation),
										Expression.Break(builder.LblBreak))));
				nextState.AddExpression(varAwaiter.Type.GetAwaiterGetResultMethod().ReturnType == typeof(void)
						? Expression.Call(varAwaiter, varAwaiter.Type.GetAwaiterGetResultMethod())
						: Expression.Assign(nextState.ResultExpression, Expression.Call(varAwaiter, varAwaiter.Type.GetAwaiterGetResultMethod())));
				currentState = nextState;
				return currentState.ResultExpression;
			}
			return base.VisitMethodCall(node);
		}
	}
}
