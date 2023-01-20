using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace bsn.AsyncLambdaExpression.Expressions {
	internal partial class ContinuationBuilder {
		protected override Expression VisitLabel(LabelExpression node) {
			var defaultValue = this.Visit(node.DefaultValue);
			var defaultValueExitState = this.currentState;
			this.currentState = this.GetLabelState(node.Target);
			defaultValueExitState.SetContinuation(this.currentState);
			if (defaultValue != null && defaultValue.Type != typeof(void)) {
				defaultValueExitState.AddExpression(
						Expression.Assign(this.currentState.ResultExpression,
								defaultValue));
			}
			return this.currentState.ResultExpression;
		}
	}
}
