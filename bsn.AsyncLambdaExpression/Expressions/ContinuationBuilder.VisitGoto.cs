using System.Linq.Expressions;

using bsn.AsyncLambdaExpression.Collections;

namespace bsn.AsyncLambdaExpression.Expressions {
	internal partial class ContinuationBuilder {
		protected override Expression VisitGoto(GotoExpression node) {
			var value = this.Visit(node.Value);
			var targetState = this.GetLabelState(node.Target);
			if (node.Target.Type != typeof(void)) {
				this.currentState.AddExpression(
						Expression.Assign(
								targetState.ResultExpression,
								value));
			}
			this.currentState.SetContinuation(targetState);
			this.currentState = new MachineState(-1, node.Type, ImmutableStack<TryInfo>.Empty, false);
			this.currentState.SetName("Goto", targetState.StateId, "Virtual");
			return Expression.Default(node.Type);
		}
	}
}
