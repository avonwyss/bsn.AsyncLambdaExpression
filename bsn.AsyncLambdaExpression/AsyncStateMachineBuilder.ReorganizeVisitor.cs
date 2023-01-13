using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace bsn.AsyncLambdaExpression {
	internal partial class AsyncStateMachineBuilder {
		private class SplitVisitor: ExpressionVisitor {
			private readonly AsyncStateMachineBuilder builder;
			internal readonly Dictionary<Expression, AsyncState> expressionStates = new(ReferenceEqualityComparer<Expression>.Default);
			internal readonly List<AsyncState> states = new();
			private AsyncState currentState;

			public SplitVisitor(AsyncStateMachineBuilder builder) {
				this.builder = builder;
			}

			protected override Expression VisitMethodCall(MethodCallExpression node) {
				if (IsAwaitExpression(node)) {
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
													builder.varContinuation),
											Expression.Break(builder.lblBreak))));
					nextState.AddExpression(varAwaiter.Type.GetAwaiterGetResultMethod().ReturnType == typeof(void)
							? Expression.Call(varAwaiter, varAwaiter.Type.GetAwaiterGetResultMethod())
							: Expression.Assign(nextState.ResultExpression, Expression.Call(varAwaiter, varAwaiter.Type.GetAwaiterGetResultMethod())));
					currentState = nextState;
					return currentState.ResultExpression;
				}
				return base.VisitMethodCall(node);
			}

			private AsyncState CreateState(Type result) {
				var state = new AsyncState(states.Count, result);
				states.Add(state);
				return state;
			}

			public override Expression Visit(Expression node) {
				if (node == null) {
					return null;
				}
				expressionStates[node] = currentState;
				var newNode = base.Visit(node);
				expressionStates[newNode] = currentState;
				return newNode;
			}

			public AsyncState Process(Expression node) {
				expressionStates.Clear();
				states.Clear();
				currentState = CreateState(typeof(void));
				var exprEnd = Visit(node);

				var methSetResult = GetTaskCompletionSourceInfo(builder.varTaskCompletionSource.Type).methSetResult;
				if (builder.Lambda.ReturnType == typeof(Task)) {
					currentState.AddExpression(exprEnd);
					currentState.AddExpression(Expression.Call(builder.varTaskCompletionSource, methSetResult, Expression.Default(typeof(Task))));
				} else {
					currentState.AddExpression(Expression.Call(builder.varTaskCompletionSource, methSetResult, exprEnd));
				}
				return currentState;
			}
		}
	}
}
