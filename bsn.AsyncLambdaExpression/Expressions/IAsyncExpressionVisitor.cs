using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace bsn.AsyncLambdaExpression.Expressions {
	public interface IAsyncExpressionVisitor {
		Expression VisitAsyncLambda<TDelegate>(AsyncLambdaExpression<TDelegate> node) where TDelegate: Delegate;

		Expression VisitAwait(AwaitExpression node);
	}
}
