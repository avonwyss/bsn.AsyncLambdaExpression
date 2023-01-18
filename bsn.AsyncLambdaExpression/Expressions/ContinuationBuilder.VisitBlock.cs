using System;
using System.Linq.Expressions;

namespace bsn.AsyncLambdaExpression.Expressions {
	internal partial class ContinuationBuilder {
		protected override Expression VisitBlock(BlockExpression node) {
			using var enumerator = node.Expressions.GetEnumerator();
			if (!enumerator.MoveNext()) {
				throw new InvalidOperationException("Empty block");
			}
			for (;;) {
				var expression = Visit(enumerator.Current);
				if (enumerator.MoveNext()) {
					currentState.AddExpression(expression);
				} else {
					return expression;
				}
			}
		}
	}
}
