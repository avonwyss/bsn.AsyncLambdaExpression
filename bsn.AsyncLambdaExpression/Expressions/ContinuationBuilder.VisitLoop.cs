using System;
using System.Linq.Expressions;

namespace bsn.AsyncLambdaExpression.Expressions {
	internal partial class ContinuationBuilder {
		protected override Expression VisitLoop(LoopExpression node) {
			var continueState = GetLabelState(node.ContinueLabel ?? Expression.Label());
			currentState.SetContinuation(continueState);
			currentState = continueState;
			var body = Visit(node.Body);
			currentState.AddExpression(body);
			currentState.SetContinuation(continueState);
			currentState = node.BreakLabel == null ? new AsyncState(-1, node.Type) : GetLabelState(node.BreakLabel);
			return currentState.ResultExpression;
		}
	}
}
