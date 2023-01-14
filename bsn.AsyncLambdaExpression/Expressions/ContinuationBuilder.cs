using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace bsn.AsyncLambdaExpression.Expressions {
	internal class ContinuationBuilder: ExpressionVisitor {
		protected struct Fiber {
			public Fiber(AsyncState originState, AsyncState entryState, AsyncState exitState, Expression expression) {
				this.OriginState = originState;
				this.EntryState = entryState;
				this.ExitState = exitState;
				this.Expression = expression;
			}

			public AsyncState OriginState {
				get;
			}

			public AsyncState EntryState {
				get;
			}

			public AsyncState ExitState {
				get;
			}

			public Expression Expression {
				get;
			}

			public bool IsAsync => EntryState != ExitState;

			public void ContinueWith(AsyncState state) {
				ExitState.AddExpression(state.ResultExpression is ParameterExpression parameter
						? Expression.Assign(parameter, Expression)
						: Expression);
				ExitState.SetContinuation(state);
			}
		}

		private readonly AsyncStateMachineBuilder builder;
		private readonly List<AsyncState> states = new();
		private AsyncState currentState;

		public ContinuationBuilder(AsyncStateMachineBuilder builder) {
			this.builder = builder;
		}

		public IReadOnlyCollection<AsyncState> States => states;

		protected Fiber VisitAsFiber(Expression expression) {
			var originState = currentState;
			try {
				var entryState = currentState = new AsyncState(currentState.StateId, currentState.ResultExpression.Type);
				var exprVisited = Visit(expression);
				var exitState = currentState;
				return new Fiber(originState, entryState, exitState, exprVisited);
			}
			finally {
				currentState = originState;
			}
		}

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

		protected override Expression VisitConditional(ConditionalExpression node) {
			var test = Visit(node.Test);
			var ifTrue = VisitAsFiber(node.IfTrue);
			var ifFalse = VisitAsFiber(node.IfFalse);
			if (!ifTrue.IsAsync && !ifFalse.IsAsync) {
				// no await inside conditional branches, proceed normally
				return node.Update(test, ifTrue.Expression, ifFalse.Expression);
			}
			currentState = CreateState(node.Type);
			ifTrue.ContinueWith(currentState);
			ifFalse.ContinueWith(currentState);
			Debug.Assert(ifTrue.OriginState == ifFalse.OriginState);
			ifTrue.OriginState.AddExpression(
					Expression.IfThenElse(
							test,
							ifTrue.EntryState.ToExpression(builder.VarState),
							ifFalse.EntryState.ToExpression(builder.VarState)));
			return currentState.ResultExpression;
		}

		protected override Expression VisitBinary(BinaryExpression node) {
			var left = Visit(node.Left);
			var right = VisitAsFiber(node.Right);
			if (right.IsAsync) {
				if (node.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse) {
					currentState = CreateState(node.Type);
					right.ContinueWith(currentState);
					// Short-cutting must be performed
					var exprEvaluate = right.EntryState.ToExpression(builder.VarState);
					var exprShortcut = Expression.Block(
							Expression.Assign(builder.VarState, Expression.Constant(currentState.StateId)),
							Expression.Assign(currentState.ResultExpression, Expression.Constant(node.NodeType != ExpressionType.AndAlso, right.Expression.Type)));
					right.OriginState.AddExpression(
							Expression.IfThenElse(
									left,
									node.NodeType == ExpressionType.AndAlso ? exprEvaluate : exprShortcut,
									node.NodeType == ExpressionType.AndAlso ? exprShortcut : exprEvaluate));
					return currentState.ResultExpression;
				}
				right.OriginState.AddExpression(right.EntryState.ToExpression(builder.VarState));
				currentState = right.ExitState;
			}
			return node.Update(left, node.Conversion, right.Expression);
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
			var methSetResult = AsyncStateMachineBuilder.GetTaskCompletionSourceInfo(builder.VarTaskCompletionSource.Type).methSetResult;
			if (builder.Lambda.ReturnType == typeof(Task)) {
				currentState.AddExpression(exprEnd);
				currentState.AddExpression(Expression.Call(builder.VarTaskCompletionSource, methSetResult, Expression.Default(typeof(Task))));
			} else {
				currentState.AddExpression(Expression.Call(builder.VarTaskCompletionSource, methSetResult, exprEnd));
			}
			return currentState;
		}
	}
}
