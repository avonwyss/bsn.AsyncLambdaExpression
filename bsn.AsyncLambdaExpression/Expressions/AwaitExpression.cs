using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace bsn.AsyncLambdaExpression.Expressions {
	public class AwaitExpression: Expression {
		protected internal AwaitExpression([NotNull] Expression awaitable) {
			this.Awaitable = awaitable ?? throw new ArgumentNullException(nameof(awaitable));
			var meth_GetAwaiter = this.Awaitable.Type.GetAwaitableGetAwaiterMethod();
			if (meth_GetAwaiter == null) {
				throw new ArgumentException($"Type '{awaitable.Type}' is not awaitable", nameof(awaitable));
			}
			this.Type = meth_GetAwaiter.ReturnType.GetAwaiterGetResultMethod().ReturnType;
		}

		public sealed override ExpressionType NodeType => ExpressionType.Extension;

		public sealed override Type Type {
			get;
		}

		public Expression Awaitable {
			get;
		}

		protected override Expression VisitChildren(ExpressionVisitor visitor) {
			return this.Update(visitor.Visit(this.Awaitable));
		}

		public AwaitExpression Update([NotNull] Expression awaitable) {
			return ReferenceEquals(awaitable, this.Awaitable)
					? this
					: new AwaitExpression(awaitable);
		}

		protected override Expression Accept(ExpressionVisitor visitor) {
			return visitor is IAsyncExpressionVisitor asyncExpressionVisitor
					? asyncExpressionVisitor.VisitAwait(this)
					: this.Update(visitor.Visit(this.Awaitable));
		}
	}
}
