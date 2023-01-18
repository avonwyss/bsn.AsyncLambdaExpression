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

		protected AsyncState GetLabelState(LabelTarget target) {
			if (!labelStates.TryGetValue(target, out var state)) {
				state = CreateState(target.Type);
				labelStates.Add(target, state);
			}
			return state;
		}

		public (AsyncState state, Expression expr) Process(Expression node) {
			states.Clear();
			labelStates.Clear();
			currentState = CreateState(typeof(void));
			var exprEnd = Visit(node);
			return (currentState, exprEnd);
		}
	}
}
