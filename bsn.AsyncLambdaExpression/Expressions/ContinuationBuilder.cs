using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using bsn.AsyncLambdaExpression.Collections;

namespace bsn.AsyncLambdaExpression.Expressions {
	internal partial class ContinuationBuilder: ExpressionVisitor {
		protected struct Fiber {
			public Fiber(AsyncState entryState, AsyncState exitState, Expression expression) {
				this.EntryState = entryState;
				this.ExitState = exitState;
				this.Expression = expression;
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
		private readonly Dictionary<LabelTarget, AsyncState> labelStates = new Dictionary<LabelTarget, AsyncState>(ReferenceEqualityComparer<LabelTarget>.Default);
		private AsyncState currentState;

		public ContinuationBuilder(AsyncStateMachineBuilder builder) {
			this.builder = builder;
		}

		public IReadOnlyCollection<AsyncState> States => states;

		protected Fiber VisitAsFiber(Expression expression, bool standalone) {
			var originState = currentState;
			try {
				var entryState = currentState = standalone 
						? CreateState(currentState.ResultExpression.Type) 
						: new AsyncState(-1, currentState.ResultExpression.Type);
				var exprVisited = Visit(expression);
				var exitState = currentState;
				return new Fiber(entryState, exitState, exprVisited);
			}
			finally {
				currentState = originState;
			}
		}

		protected AsyncState CreateState(Type result) {
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
