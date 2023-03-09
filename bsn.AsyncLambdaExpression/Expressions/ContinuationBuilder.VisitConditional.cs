using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace bsn.AsyncLambdaExpression.Expressions {
	internal partial class ContinuationBuilder {
		protected override Expression VisitConditional(ConditionalExpression node) {
			var test = this.Visit(node.Test);
			var ifTrue = this.VisitAsFiber(node.IfTrue, FiberMode.Continuous);
			var ifFalse = this.VisitAsFiber(node.IfFalse, FiberMode.Continuous);
			if (!ifTrue.IsAsync && !ifFalse.IsAsync) {
				// no await inside conditional branches, proceed normally
				return node.Update(test, ifTrue.Expression, ifFalse.Expression);
			}
			var testExitState = this.currentState;
			ifTrue.SetName("Conditional", testExitState.StateId, "True");
			ifFalse.SetName("Conditional", testExitState.StateId, "False");
			this.currentState = this.CreateState(node.Type);
			this.currentState.SetName("Conditional", testExitState.StateId, "Merge");
			ifTrue.ContinueWith(this.currentState);
			ifFalse.ContinueWith(this.currentState);
			testExitState.AddExpression(
					Expression.IfThenElse(
							test,
							ifTrue.EntryState.ToExpression(this.vars),
							ifFalse.EntryState.ToExpression(this.vars)));
			return this.currentState.ResultExpression;
		}
	}
}
