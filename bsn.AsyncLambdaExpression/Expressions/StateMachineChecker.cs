using System;
using System.Linq.Expressions;
using System.Xml;

namespace bsn.AsyncLambdaExpression.Expressions {
	internal sealed class StateMachineChecker: ExpressionVisitor, IAsyncExpressionVisitor, IIteratorExpressionVisitor {
		private readonly bool labelAndGotoAreStates;

		protected override Expression VisitLabel(LabelExpression node) {
			if (this.labelAndGotoAreStates) {
				// Plain labels must act on the state machine
				this.RequiresStateMachine = true;
				return node;
			}
			return base.VisitLabel(node);
		}

		protected override Expression VisitGoto(GotoExpression node) {
			if (this.labelAndGotoAreStates && node.Kind is not (GotoExpressionKind.Break or GotoExpressionKind.Continue)) {
				// Goto and Return must act on the state machine
				this.RequiresStateMachine = true;
				return node;
			}
			return base.VisitGoto(node);
		}

		public override Expression Visit(Expression node) {
			if (this.RequiresStateMachine) {
				// If we already know that we need a state machine, no need to look deeper
				return node;
			}
			return base.Visit(node);
		}

		public StateMachineChecker(bool labelAndGotoAreStates) {
			this.labelAndGotoAreStates = labelAndGotoAreStates;
		}

		public bool RequiresStateMachine {
			get;
			private set;
		}

		protected override Expression VisitLambda<T>(Expression<T> node) {
			// Don't traverse lambda expressions
			return node;
		}

		public Expression VisitAsyncLambda<TDelegate>(AsyncLambdaExpression<TDelegate> node) where TDelegate: Delegate {
			// Don't traverse lambda expressions
			return node;
		}

		public Expression VisitAwait(AwaitExpression node) {
			this.RequiresStateMachine = true;
			return node;
		}

		public Expression VisitIteratorLambda<TDelegate>(IteratorLambdaExpression<TDelegate> node) where TDelegate: Delegate {
			// Don't traverse lambda expressions
			return node;
		}

		public Expression VisitYieldReturn(YieldReturnExpression node) {
			this.RequiresStateMachine = true;
			return node;
		}
	}
}
