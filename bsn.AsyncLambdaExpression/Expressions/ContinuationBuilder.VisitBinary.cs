using System.Linq.Expressions;

namespace bsn.AsyncLambdaExpression.Expressions {
	internal partial class ContinuationBuilder {
		protected override Expression VisitBinary(BinaryExpression node) {
			var left = Visit(node.Left);
			var right = VisitAsFiber(node.Right, false);
			if (!right.IsAsync) {
				return node.Update(left, node.Conversion, right.Expression);
			}
			var leftExitState = currentState;
			if (node.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse) {
				currentState = CreateState(node.Type);
				right.ContinueWith(currentState);
				// Short-cutting must be performed
				var exprEvaluate = right.EntryState.ToExpression(builder.VarState);
				var exprShortcut = Expression.Block(
						Expression.Assign(builder.VarState, Expression.Constant(currentState.StateId)),
						Expression.Assign(currentState.ResultExpression, Expression.Constant(node.NodeType != ExpressionType.AndAlso, right.Expression.Type)));
				leftExitState.AddExpression(
						Expression.IfThenElse(
								left,
								node.NodeType == ExpressionType.AndAlso ? exprEvaluate : exprShortcut,
								node.NodeType == ExpressionType.AndAlso ? exprShortcut : exprEvaluate));
				leftExitState.OmitStateAssignment();
				return currentState.ResultExpression;
			}
			leftExitState.AddExpression(right.EntryState.ToExpression(builder.VarState));
			currentState = right.ExitState;
			return node.Update(left, node.Conversion, right.Expression);
		}
	}
}
