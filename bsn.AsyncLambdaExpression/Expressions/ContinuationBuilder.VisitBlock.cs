using System;
using System.Linq.Expressions;

namespace bsn.AsyncLambdaExpression.Expressions {
	internal partial class ContinuationBuilder {
		protected override Expression VisitBlock(BlockExpression node) {
			if (!node.ContainsAsyncCode(true)) {
				return node;
			}
			using var enumerator = node.Expressions.GetEnumerator();
			if (!enumerator.MoveNext()) {
				throw new InvalidOperationException("Empty block");
			}
			for (;;) {
				var expression = this.Visit(enumerator.Current);
				if (enumerator.MoveNext()) {
					this.currentState.AddExpression(expression);
				} else {
					return expression;
				}
			}
		}
	}
}
