using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace bsn.AsyncLambdaExpression {
	internal struct CatchInfo {
		public CatchInfo(AsyncState bodyState, ParameterExpression variable, Type test, Expression filter) {
			Debug.Assert(bodyState.StateId > 0 && typeof(Exception).IsAssignableFrom(test) && (variable == null || variable.Type == test) && (filter == null || filter.Type == typeof(bool)));
			this.BodyState = bodyState;
			this.Variable = variable;
			this.Test = test;
			this.Filter = filter;
		}

		public AsyncState BodyState {
			get;
		}

		public ParameterExpression Variable {
			get;
		}

		public Type Test {
			get;
		}

		public Expression Filter {
			get;
		}
	}
}
