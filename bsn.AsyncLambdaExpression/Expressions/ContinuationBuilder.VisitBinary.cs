using System.Linq.Expressions;

namespace bsn.AsyncLambdaExpression.Expressions {
	internal partial class ContinuationBuilder {
		protected override Expression VisitBinary(BinaryExpression node) {
			var left = this.Visit(node.Left);
			var right = this.VisitAsFiber(node.Right, false);
			right.SetName("Binary", this.currentState.StateId, "Right");
			if (!right.IsAsync) {
				return node.Update(left, node.Conversion, right.Expression);
			}
			var leftExitState = this.currentState;
			if (node.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse) {
				this.currentState = this.CreateState(node.Type);
				this.currentState.SetName("Binary", leftExitState.StateId, "Shortcut Merge");
				right.ContinueWith(this.currentState);
				// Short-cutting must be performed
				var exprEvaluate = right.EntryState.ToExpression(this.vars);
				var exprShortcut = Expression.Block(
						Expression.Assign(this.vars.VarState, Expression.Constant(this.currentState.StateId)),
						Expression.Assign(this.currentState.ResultExpression, Expression.Constant(node.NodeType != ExpressionType.AndAlso, right.Expression.Type)));
				leftExitState.AddExpression(
						Expression.IfThenElse(
								left,
								node.NodeType == ExpressionType.AndAlso ? exprEvaluate : exprShortcut,
								node.NodeType == ExpressionType.AndAlso ? exprShortcut : exprEvaluate));
				return this.currentState.ResultExpression;
			}
			leftExitState.AddExpression(right.EntryState.ToExpression(this.vars));
			this.currentState = right.ExitState;
			return node.Update(left, node.Conversion, right.Expression);
		}
	}
}
