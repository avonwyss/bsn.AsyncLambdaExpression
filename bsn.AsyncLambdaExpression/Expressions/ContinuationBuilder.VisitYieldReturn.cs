using System.Linq.Expressions;

namespace bsn.AsyncLambdaExpression.Expressions {
	internal partial class ContinuationBuilder {
		public Expression VisitYieldReturn(YieldReturnExpression node) {
			var nextState = this.CreateState(typeof(void));
			nextState.SetName("Yield", this.currentState.StateId, "Return");
			this.currentState.SetContinuation(nextState);
			var exprResult = this.Visit(node.Yield);
			this.currentState.AddExpression(
					Expression.Assign(
							Expression.Field(this.vars.VarCurrent, this.vars.VarCurrent.Type.GetStrongBoxValueField()),
							exprResult));
			this.currentState.AddExpression(
					Expression.Break(this.vars.LblBreak,
							Expression.Constant(true)));
			this.currentState = nextState;
			return this.currentState.ResultExpression;
		}
	}
}
