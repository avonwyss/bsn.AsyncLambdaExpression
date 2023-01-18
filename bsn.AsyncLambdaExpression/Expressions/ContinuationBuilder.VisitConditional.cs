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
			currentState = CreateState(node.Type);
			ifTrue.ContinueWith(currentState);
			ifFalse.ContinueWith(currentState);
			testExitState.AddExpression(
					Expression.IfThenElse(
							test,
							ifTrue.EntryState.ToExpression(builder.VarState),
							ifFalse.EntryState.ToExpression(builder.VarState)));
			testExitState.OmitStateAssignment();
			return currentState.ResultExpression;
		}
	}
}
