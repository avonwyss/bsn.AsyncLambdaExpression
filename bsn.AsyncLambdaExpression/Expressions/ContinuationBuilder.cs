using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using bsn.AsyncLambdaExpression.Collections;

namespace bsn.AsyncLambdaExpression.Expressions {
	internal partial class ContinuationBuilder: ExpressionVisitor, IAsyncExpressionVisitor, IIteratorExpressionVisitor {
		protected enum FiberMode {
			Continuous,
			Standalone,
			Finally
		}

		protected struct Fiber {
			public Fiber(MachineState entryState, MachineState exitState, Expression expression) {
				this.EntryState = entryState;
				this.ExitState = exitState;
				this.Expression = expression;
			}

			public MachineState EntryState {
				get;
			}

			public MachineState ExitState {
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

			public void ContinueWith(MachineState state) {
				this.AssignResult(state);
				this.ExitState.SetContinuation(state);
			}

			public void AssignResult(MachineState state) {
				this.ExitState.AddExpression(state?.ResultExpression is ParameterExpression parameter
						? Expression.Assign(parameter, this.Expression)
						: this.Expression);
			}
		}

		private readonly IStateMachineVariables vars;
		private readonly List<MachineState> states = new();
		private readonly Dictionary<LabelTarget, MachineState> labelStates = new(ReferenceEqualityComparer<LabelTarget>.Default);
		private MachineState currentState;
		private MachineState rethrowState;

		public ContinuationBuilder(IStateMachineVariables vars) {
			this.vars = vars;
		}

		public IReadOnlyCollection<MachineState> States => this.states;

		protected Fiber VisitAsFiber(Expression expression, FiberMode mode, ImmutableStack<TryInfo> tryInfos = null) {
			var originState = this.currentState;
			try {
				var entryState = this.currentState = mode == FiberMode.Continuous
						? new MachineState(-1, this.currentState.ResultExpression.Type, tryInfos ?? originState.TryInfos, false)
						: this.CreateState(this.currentState.ResultExpression.Type, tryInfos ?? originState.TryInfos, mode == FiberMode.Finally);
				var exprVisited = this.Visit(expression);
				var exitState = this.currentState;
				var fiber = new Fiber(entryState, exitState, exprVisited);
				// fiber.SetName("Unnamed", entryState.StateId, "");
				return fiber;
			} finally {
				this.currentState = originState;
			}
		}

		protected MachineState CreateState(Type result, ImmutableStack<TryInfo> tryInfos = null, bool finallyState = false) {
			var state = new MachineState(this.states.Count, result, tryInfos ?? this.currentState.TryInfos, finallyState);
			this.states.Add(state);
			return state;
		}

		protected MachineState GetLabelState(LabelTarget target) {
			if (!this.labelStates.TryGetValue(target, out var state)) {
				state = this.CreateState(target.Type);
				state.SetName("Label Target", state.StateId, target.Name);
				this.labelStates.Add(target, state);
			}
			return state;
		}

		public (MachineState state, Expression expr) Process(Expression node) {
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
