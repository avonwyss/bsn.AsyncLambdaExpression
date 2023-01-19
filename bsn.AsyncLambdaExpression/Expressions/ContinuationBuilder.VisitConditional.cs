using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

using bsn.AsyncLambdaExpression.Collections;

namespace bsn.AsyncLambdaExpression.Expressions {
	internal partial class ContinuationBuilder {
		protected override Expression VisitConditional(ConditionalExpression node) {
			var test = Visit(node.Test);
			var ifTrue = VisitAsFiber(node.IfTrue, false);
			var ifFalse = VisitAsFiber(node.IfFalse, false);
			if (!ifTrue.IsAsync && !ifFalse.IsAsync) {
				// no await inside conditional branches, proceed normally
				return node.Update(test, ifTrue.Expression, ifFalse.Expression);
			}
			var testExitState = currentState;
			ifTrue.SetName("Conditional", testExitState.StateId, "True");
			ifFalse.SetName("Conditional", testExitState.StateId, "False");
			currentState = CreateState(node.Type);
			currentState.SetName("Conditional", testExitState.StateId, "Merge");
			ifTrue.ContinueWith(currentState);
			ifFalse.ContinueWith(currentState);
			testExitState.AddExpression(
					Expression.IfThenElse(
							test,
							ifTrue.EntryState.ToExpression(vars),
							ifFalse.EntryState.ToExpression(vars)));
			return currentState.ResultExpression;
		}
	}
}
