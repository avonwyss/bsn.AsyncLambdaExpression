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

			public bool IsAsync => this.EntryState != this.ExitState;

			[Conditional("DEBUG")]
			internal void SetName(string kind, int groupId, string detail) {
				this.EntryState.SetName(kind, groupId, $"Fiber {detail}");
			}

			public void ContinueWith(AsyncState state) {
				this.AssignResult(state);
				this.ExitState.SetContinuation(state);
			}

			public void AssignResult(AsyncState state) {
				this.ExitState.AddExpression(state?.ResultExpression is ParameterExpression parameter
						? Expression.Assign(parameter, this.Expression)
						: this.Expression);
			}
		}

		private readonly IAsyncStateMachineVariables vars;
		private readonly List<AsyncState> states = new();
		private readonly Dictionary<LabelTarget, AsyncState> labelStates = new(ReferenceEqualityComparer<LabelTarget>.Default);
		private AsyncState currentState;
		private AsyncState rethrowState;

		public ContinuationBuilder(IAsyncStateMachineVariables vars) {
			this.vars = vars;
		}

		public IReadOnlyCollection<AsyncState> States => this.states;

		protected Fiber VisitAsFiber(Expression expression, bool standalone, ImmutableStack<TryInfo> tryInfos = null) {
			var originState = this.currentState;
			try {
				var entryState = this.currentState = standalone
						? this.CreateState(this.currentState.ResultExpression.Type, tryInfos ?? originState.TryInfos)
						: new AsyncState(-1, this.currentState.ResultExpression.Type, tryInfos ?? originState.TryInfos);
				var exprVisited = this.Visit(expression);
				var exitState = this.currentState;
				var fiber = new Fiber(entryState, exitState, exprVisited);
				// fiber.SetName("Unnamed", entryState.StateId, "");
				return fiber;
			} finally {
				this.currentState = originState;
			}
		}

		protected AsyncState CreateState(Type result, ImmutableStack<TryInfo> tryInfos = null) {
			var state = new AsyncState(this.states.Count, result, tryInfos ?? this.currentState.TryInfos);
			this.states.Add(state);
			return state;
		}

		protected AsyncState GetLabelState(LabelTarget target) {
			if (!this.labelStates.TryGetValue(target, out var state)) {
				state = this.CreateState(target.Type);
				state.SetName("Label Target", state.StateId, target.Name);
				this.labelStates.Add(target, state);
			}
			return state;
		}

		public (AsyncState state, Expression expr) Process(Expression node) {
			this.states.Clear();
			this.labelStates.Clear();
			this.rethrowState = null;
			this.currentState = this.CreateState(typeof(void), ImmutableStack<TryInfo>.Empty);
			this.currentState.SetName("Entry", 0, "");
			var exprEnd = this.Visit(node);
			return (this.currentState, exprEnd);
		}
	}
}
