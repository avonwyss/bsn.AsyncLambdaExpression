using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace bsn.AsyncLambdaExpression.Expressions {
	internal partial class ContinuationBuilder {
		public Expression VisitAsyncLambda<TDelegate>(AsyncLambdaExpression<TDelegate> node) where TDelegate: Delegate {
			return node.BuildLambdaExpression(this.vars.DebugInfoGenerator);
		}

		public Expression VisitIteratorLambda<TDelegate>(IteratorLambdaExpression<TDelegate> node) where TDelegate: Delegate {
			return node.BuildLambdaExpression(this.vars.DebugInfoGenerator);
		}
	}
}
