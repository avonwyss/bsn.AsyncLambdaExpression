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

			[Conditional("DEBUG")]
			internal void SetName(string kind, int groupId, string detail) {
				EntryState.SetName(kind, groupId, $"Fiber {detail}");
			}

			public void ContinueWith(AsyncState state) {
				AssignResult(state);
				ExitState.SetContinuation(state);
			}

			public void AssignResult(AsyncState state) {
				ExitState.AddExpression(state?.ResultExpression is ParameterExpression parameter
						? Expression.Assign(parameter, Expression)
						: Expression);
			}
		}

		private readonly IAsyncStateMachineVariables vars;
		private readonly List<AsyncState> states = new();
		private readonly Dictionary<LabelTarget, AsyncState> labelStates = new Dictionary<LabelTarget, AsyncState>(ReferenceEqualityComparer<LabelTarget>.Default);
		private AsyncState currentState;
		private AsyncState rethrowState;

		public ContinuationBuilder(IAsyncStateMachineVariables vars) {
			this.vars = vars;
		}

		public IReadOnlyCollection<AsyncState> States => states;

		protected Fiber VisitAsFiber(Expression expression, bool standalone, ImmutableStack<TryInfo> tryInfos = null) {
			var originState = currentState;
			try {
				var entryState = currentState = standalone 
						? CreateState(currentState.ResultExpression.Type, tryInfos ?? originState.TryInfos) 
						: new AsyncState(-1, currentState.ResultExpression.Type, tryInfos ?? originState.TryInfos);
				var exprVisited = Visit(expression);
				var exitState = currentState;
				var fiber = new Fiber(entryState, exitState, exprVisited);
				// fiber.SetName("Unnamed", entryState.StateId, "");
				return fiber;
			}
			finally {
				currentState = originState;
			}
		}

		protected AsyncState CreateState(Type result, ImmutableStack<TryInfo> tryInfos = null) {
			var state = new AsyncState(states.Count, result, tryInfos ?? currentState.TryInfos);
			states.Add(state);
			return state;
		}

		protected AsyncState GetLabelState(LabelTarget target) {
			if (!labelStates.TryGetValue(target, out var state)) {
				state = CreateState(target.Type);
				state.SetName("Label Target", state.StateId, target.Name);
				labelStates.Add(target, state);
			}
			return state;
		}

		public (AsyncState state, Expression expr) Process(Expression node) {
			states.Clear();
			labelStates.Clear();
			rethrowState = null;
			currentState = CreateState(typeof(void), ImmutableStack<TryInfo>.Empty);
			currentState.SetName("Entry", 0, "");
			var exprEnd = Visit(node);
			return (currentState, exprEnd);
		}
	}
}
