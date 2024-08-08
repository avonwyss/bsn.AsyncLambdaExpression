using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace bsn.AsyncLambdaExpression.Expressions {
	public class YieldReturnExpression: Expression {
		protected internal YieldReturnExpression([NotNull] Expression yield) {
			this.Yield = yield ?? throw new ArgumentNullException(nameof(yield));
		}

		public sealed override ExpressionType NodeType => ExpressionType.Extension;

		public sealed override Type Type => typeof(void);

		public Expression Yield {
			get;
		}

		protected override Expression VisitChildren(ExpressionVisitor visitor) {
			return this.Update(visitor.Visit(this.Yield));
		}

		public YieldReturnExpression Update([NotNull] Expression yield) {
			return ReferenceEquals(yield, this.Yield)
					? this
					: new YieldReturnExpression(yield);
		}

		protected override Expression Accept(ExpressionVisitor visitor) {
			return visitor is IIteratorExpressionVisitor iteratorExpressionVisitor
					? iteratorExpressionVisitor.VisitYieldReturn(this)
					: this.Update(visitor.Visit(this.Yield));
		}
	}
}
