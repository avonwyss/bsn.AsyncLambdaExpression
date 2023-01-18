using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace bsn.AsyncLambdaExpression.Expressions {
	internal partial class ContinuationBuilder {
		protected override Expression VisitLabel(LabelExpression node) {
			var defaultValue = Visit(node.DefaultValue);
			var defaultValueExitState = currentState;
			currentState = GetLabelState(node.Target);
			defaultValueExitState.SetContinuation(currentState);
			if (defaultValue != null && defaultValue.Type != typeof(void)) {
				defaultValueExitState.AddExpression(
						Expression.Assign(
								currentState.ResultExpression,
								defaultValue));
			}
			return currentState.ResultExpression;
		}
	}
}
