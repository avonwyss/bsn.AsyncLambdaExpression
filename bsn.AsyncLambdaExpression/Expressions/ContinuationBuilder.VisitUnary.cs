using System.Linq.Expressions;

namespace bsn.AsyncLambdaExpression.Expressions {
	internal partial class ContinuationBuilder {
		protected override Expression VisitUnary(UnaryExpression node) {
			// ReSharper disable once ConditionIsAlwaysTrueOrFalse
			if (node.NodeType == ExpressionType.Throw && node.Operand == null) {
				return node.Update(vars.VarException);
			}
			return base.VisitUnary(node);
		}
	}
}
