using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace bsn.AsyncLambdaExpression {
	internal class AsyncState {
		private readonly List<Expression> expressions = new(1);
		private readonly List<ParameterExpression> variables = new(1);
		private bool omitStateAssignment;

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

		public AsyncState(int stateId, Type result) {
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
			Debug.Assert(Continuation == null && !omitStateAssignment);
			Continuation = state;
		}

		public void AddExpression(Expression expression) {
			expressions.Add(expression);
		}

		public void AddVariable(ParameterExpression variable) {
			variables.Add(variable);
		}

		public void OmitStateAssignment() {
			Debug.Assert(Continuation == null);
			omitStateAssignment = true;
		}

		public Expression ToExpression(ParameterExpression varState) {
			Debug.Assert(varState != null);
			var expressions =
					omitStateAssignment
							? this.expressions
							: this.expressions.Prepend(
									Expression.Assign(
											varState,
											Expression.Constant(Continuation?.StateId ?? -1)));
			return Expression.Block(variables, expressions);
		}

		public override string ToString() {
			return Continuation == null ? StateId.ToString() : $"{StateId} => {Continuation.StateId}";
		}
	}
}
