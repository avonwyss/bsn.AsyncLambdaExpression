using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace bsn.AsyncLambdaExpression.Expressions {
	internal sealed class AsyncConverter: ExpressionVisitor, IAsyncExpressionVisitor {
		private readonly bool debug;

		public AsyncConverter(bool debug) {
			this.debug = debug;
		}

		public Expression VisitAsyncLambda<TDelegate>(AsyncLambdaExpression<TDelegate> node) where TDelegate: Delegate {
			return node.BuildLambdaExpression(null);
		}

		public Expression VisitAwait(AwaitExpression node) {
			throw new NotImplementedException();
		}
	}
}
