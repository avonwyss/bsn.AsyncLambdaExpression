using System;
using System.Linq.Expressions;

namespace bsn.AsyncLambdaExpression.Expressions {
	internal sealed class AsyncChecker: ExpressionVisitor {
		private readonly bool labelAndGotoAreAsync;

		protected override Expression VisitLabel(LabelExpression node) {
			if (this.labelAndGotoAreAsync) {
				// Plain labels must act on the state machine
				this.HasAsyncCode = true;
				return node;
			}
			return base.VisitLabel(node);
		}

		protected override Expression VisitGoto(GotoExpression node) {
			if (this.labelAndGotoAreAsync && node.Kind is not (GotoExpressionKind.Break or GotoExpressionKind.Continue)) {
				// Goto and Return must act on the state machine
				this.HasAsyncCode = true;
				return node;
			}
			return base.VisitGoto(node);
		}

		protected override Expression VisitMethodCall(MethodCallExpression node) {
			if (AsyncExpressionExtensions.IsAwaitMethod(node.Method) || IteratorExpressionExtensions.IsYieldReturnMethod(node.Method)) {
				this.HasAsyncCode = true;
				return node;
			}
			return base.VisitMethodCall(node);
		}

		public override Expression Visit(Expression node) {
			if (this.HasAsyncCode) {
				return node;
			}
			return base.Visit(node);
		}

		public AsyncChecker(bool labelAndGotoAreAsync) {
			this.labelAndGotoAreAsync = labelAndGotoAreAsync;
		}

		public bool HasAsyncCode {
			get;
			private set;
		}
	}
}
