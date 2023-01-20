using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace bsn.AsyncLambdaExpression.Expressions {
	internal partial class ContinuationBuilder {
		protected override Expression VisitLambda<T>(Expression<T> node) {
			if (typeof(Task).IsAssignableFrom(node.Body.Type) && node.Body.ContainsAsyncCode(false)) {
				// Make sure that there is no await in the code
				throw new NotSupportedException("Nested async lambdas are not supported.");
			}
			return node;
		}
	}
}
