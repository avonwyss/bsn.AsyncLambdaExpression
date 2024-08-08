using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace bsn.AsyncLambdaExpression.Expressions {
	public interface IIteratorExpressionVisitor {
		Expression VisitIteratorLambda<TDelegate>(IteratorLambdaExpression<TDelegate> node) where TDelegate: Delegate;

		Expression VisitYieldReturn(YieldReturnExpression node);
	}
}
