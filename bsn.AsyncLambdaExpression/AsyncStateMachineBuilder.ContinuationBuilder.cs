using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace bsn.AsyncLambdaExpression {
	internal partial class AsyncStateMachineBuilder {
		private class ContinuationBuilder: ExpressionVisitor {
			private readonly AsyncStateMachineBuilder builder;
			private readonly List<AsyncState> states = new();
			private AsyncState currentState;

			public ContinuationBuilder(AsyncStateMachineBuilder builder) {
				this.builder = builder;
			}

			public IReadOnlyCollection<AsyncState> States => states;

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

			protected override Expression VisitConditional(ConditionalExpression node) {
				var test = Visit(node.Test);
				var testExitState = currentState;
				var ifTrueEntryState = currentState = new AsyncState(currentState.StateId, currentState.ResultExpression.Type);
				var ifTrue = Visit(node.IfTrue);
				var ifTrueExitState = currentState;
				var ifFalseEntryState = currentState = new AsyncState(currentState.StateId, currentState.ResultExpression.Type);
				var ifFalse = Visit(node.IfFalse);
				var ifFalseExitState = currentState;
				if (ifTrueEntryState == ifTrueExitState && ifFalseEntryState == ifFalseExitState) {
					// no await inside conditional branches, proceed normally
					currentState = testExitState;
					return node.Update(test, ifTrue, ifFalse);
				}
				currentState = CreateState(node.Type);
				ifTrueExitState.AddExpression(node.Type == typeof(void)
						? ifTrue
						: Expression.Assign(currentState.ResultExpression, ifTrue));
				ifTrueExitState.SetContinuation(currentState);
				ifFalseExitState.AddExpression(node.Type == typeof(void)
						? ifFalse
						: Expression.Assign(currentState.ResultExpression, ifFalse));
				ifFalseExitState.SetContinuation(currentState);
				testExitState.AddExpression(
						Expression.IfThenElse(
								test,
								ifTrueEntryState.ToExpression(builder.varState),
								ifFalseEntryState.ToExpression(builder.varState)));
				return currentState.ResultExpression;
			}

			protected override Expression VisitBinary(BinaryExpression node) {
				var left = Visit(node.Left);
				Expression right;
				if (node.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse) {
					var leftExitState = currentState;
					var rightEntryState = currentState = new AsyncState(currentState.StateId, currentState.ResultExpression.Type);
					right = Visit(node.Right);
					var rightExitState = currentState;
					if (rightExitState != rightEntryState) {
						// Short-cutting must be performed
						currentState = CreateState(node.Type);
						rightExitState.AddExpression(Expression.Assign(currentState.ResultExpression, right));
						rightExitState.SetContinuation(currentState);
						var exprEvaluate = rightEntryState.ToExpression(builder.varState);
						var exprShortcut = Expression.Block(
								Expression.Assign(builder.varState, Expression.Constant(currentState.StateId)),
								Expression.Assign(currentState.ResultExpression, Expression.Constant(node.NodeType != ExpressionType.AndAlso, right.Type)));
						leftExitState.AddExpression(
								Expression.IfThenElse(
										left,
										node.NodeType == ExpressionType.AndAlso ? exprEvaluate : exprShortcut,
										node.NodeType == ExpressionType.AndAlso ? exprShortcut : exprEvaluate));
						return currentState.ResultExpression;
					}
					// restore state
					currentState = leftExitState;
				} else {
					right = Visit(node.Right);
				}
				return node.Update(left, node.Conversion, right);
			}

			private AsyncState CreateState(Type result) {
				var state = new AsyncState(states.Count, result);
				states.Add(state);
				return state;
			}

			public AsyncState Process(Expression node) {
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
