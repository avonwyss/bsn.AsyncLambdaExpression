using System.Linq.Expressions;

namespace bsn.AsyncLambdaExpression.Expressions {
	internal partial class ContinuationBuilder {
		protected override Expression VisitGoto(GotoExpression node) {
			var value = Visit(node.Value);
			var targetState = GetLabelState(node.Target);
			if (node.Target.Type != typeof(void)) {
				currentState.AddExpression(
						Expression.Assign(
								targetState.ResultExpression,
								value));
			}
			currentState.SetContinuation(targetState);
			currentState = new AsyncState(-1, node.Type);
			return Expression.Default(node.Type);
		}
	}
}
