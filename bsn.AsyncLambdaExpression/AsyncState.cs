using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

using bsn.AsyncLambdaExpression.Collections;

namespace bsn.AsyncLambdaExpression {
	internal class AsyncState {
		private readonly List<Expression> expressions = new(1);
		private readonly List<ParameterExpression> variables = new(1);

#if DEBUG
		internal string Name {
			get;
			private set;
		}
#endif
		[Conditional("DEBUG")]
		internal void SetName(string kind, int groupId, string detail) {
#if DEBUG
			Debug.Assert(string.IsNullOrEmpty(Name));
			Name = $"{kind} {groupId} {detail}".TrimEnd();
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

		public AsyncState Continuation {
			get;
			private set;
		}

		public IReadOnlyCollection<ParameterExpression> Variables => variables;

		public IReadOnlyCollection<Expression> Expressions => expressions;

		public AsyncState(int stateId, Type result, ImmutableStack<TryInfo> tryInfos) {
			this.TryInfos = tryInfos;
			StateId = stateId;
			if (result == null || result == typeof(void)) {
				ResultExpression = Expression.Empty();
			} else {
				var varResult = Expression.Variable(result, $"result:{stateId}");
				ResultExpression = varResult;
				variables.Add(varResult);
			}
		}

		public void SetContinuation(AsyncState state) {
			Debug.Assert(Continuation == null);
			Continuation = state;
		}

		public void AddExpression(Expression expression) {
			expressions.Add(expression);
		}

		public Expression ToExpression(IAsyncStateMachineVariables vars) {
			Debug.Assert(vars != null);
			var expressions = this.expressions.ToList();
			if (Continuation != null) {
				expressions.Insert(0,
						Expression.Assign(
								vars.VarState,
								Expression.Constant(Continuation.StateId)));
				if (Continuation == TryInfos.PeekOrDefault().FinallyState) {
					expressions.Insert(0,
							Expression.Assign(
									vars.VarResumeState,
									Expression.Constant(TryInfos.Peek().ExitState.StateId)));
				}
			}
			return Expression.Block(variables, expressions);
		}

		public override string ToString() {
			return Continuation == null ? StateId.ToString() : $"{StateId} => {Continuation.StateId}";
		}
	}
}
