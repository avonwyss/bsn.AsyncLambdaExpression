using System;
using System.Linq.Expressions;

namespace bsn.AsyncLambdaExpression.Expressions {
	internal partial class ContinuationBuilder {
		protected override Expression VisitLambda<T>(Expression<T> node) {
			throw new NotImplementedException();
		}
	}
}
