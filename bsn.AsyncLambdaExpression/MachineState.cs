using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

using bsn.AsyncLambdaExpression.Collections;

namespace bsn.AsyncLambdaExpression {
	internal class MachineState {
		private readonly List<Expression> expressions = new(1);
		private readonly List<ParameterExpression> variables = new(1);

		public bool FinallyState {
			get;
		}

#if DEBUG
		internal string Name {
			get;
			private set;
		}
#endif
		[Conditional("DEBUG")]
		internal void SetName(string kind, int groupId, string detail) {
#if DEBUG
			Debug.Assert(string.IsNullOrEmpty(this.Name));
			this.Name = $"{kind} {groupId} {detail}".TrimEnd();
#endif
		}

		public ImmutableStack<TryInfo> TryInfos {
			get;
		}

		public int StateId {
			get;
		}

		public Expression ResultExpression {
			get;
		}

		public MachineState Continuation {
			get;
			private set;
		}

		public IReadOnlyCollection<ParameterExpression> Variables => this.variables;

		public IReadOnlyCollection<Expression> Expressions => this.expressions;

		public MachineState(int stateId, Type result, ImmutableStack<TryInfo> tryInfos, bool finallyState) {
			this.TryInfos = tryInfos;
			this.FinallyState = finallyState;
			this.StateId = stateId;
			if (result == null || result == typeof(void)) {
				this.ResultExpression = Expression.Empty();
			} else {
				var varResult = Expression.Variable(result, $"result:{stateId}");
				this.ResultExpression = varResult;
				this.variables.Add(varResult);
			}
		}

		public void SetContinuation(MachineState state) {
			Debug.Assert(this.Continuation == null);
			this.Continuation = state;
		}

		public void AddExpression(Expression expression) {
			this.expressions.Add(expression);
		}

		public BlockExpression ToExpression(IStateMachineVariables vars) {
			Debug.Assert(vars != null);
			var expressions = this.expressions.ToList();
			if (this.Continuation != null) {
				expressions.Insert(0,
						Expression.Assign(
								vars.VarState,
								Expression.Constant(this.Continuation.StateId)));
				if (this.Continuation == this.TryInfos.PeekOrDefault().FinallyState) {
					expressions.Insert(0,
							Expression.Assign(
									vars.VarResumeState,
									Expression.Constant(this.TryInfos.Peek().ExitState.StateId)));
				}
			}
			return Expression.Block(this.variables, expressions);
		}

		public override string ToString() {
			return this.Continuation == null ? this.StateId.ToString() : $"{this.StateId} => {this.Continuation.StateId}";
		}
	}
}
