using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using bsn.AsyncLambdaExpression.Collections;

namespace bsn.AsyncLambdaExpression.Expressions {
	internal partial class ContinuationBuilder {
		protected override Expression VisitSwitch(SwitchExpression node) {
			var switchValue = Visit(node.SwitchValue);
			var cases = node
					.Cases
					.SelectMany(sc => sc.TestValues.Select<Expression, (Fiber testValue, Expression body)>(tv => (VisitAsFiber(tv, false), sc.Body)))
					.ToArray();
			var hasAsyncTestValues = cases.Any(c => c.testValue.IsAsync);
			var caseBodies = cases
					.Select(c => c.body)
					.Distinct()
					.ToDictionary(b => b, b => VisitAsFiber(b, hasAsyncTestValues));
			var defaultBody = VisitAsFiber(node.DefaultBody, hasAsyncTestValues);
			if (!hasAsyncTestValues && !defaultBody.IsAsync && !caseBodies.Values.Any(b => b.IsAsync)) {
				// No async test values or bodies, emit plain switch-case
				return node.Update(switchValue, cases
								.GroupSame(c => c.body, c => c.testValue, ReferenceEqualityComparer<Expression>.Default)
								.Select(g => Expression.SwitchCase(caseBodies[g.Key].Expression, g.Select(f => f.Expression))),
						defaultBody.Expression);
			}
			// Create merge state, and set up continuation
			var caseState = currentState;
			currentState = CreateState(node.Type);
			defaultBody.ContinueWith(currentState);
			foreach (var caseBodyValue in caseBodies.Values) {
				caseBodyValue.ContinueWith(currentState);
			}
			if (!hasAsyncTestValues) {
				// No async test values, emit a void switch-case and merge the result in a distinct state
				caseState.AddExpression(
						Expression.Switch(typeof(void), switchValue, defaultBody.EntryState.ToExpression(builder.VarState), node.Comparison, cases
								.GroupSame(c => c.body, c => c.testValue, ReferenceEqualityComparer<Expression>.Default)
								.Select(g => Expression.SwitchCase(caseBodies[g.Key].EntryState.ToExpression(builder.VarState), g.Select(f => f.Expression)))));
				return currentState.ResultExpression;
			}
			// With async test values, emit switch cases spread over multiple states
			var switchCases = new List<(Expression testValue, AsyncState targetState)>();

			void FlushSwitchCases(Expression defaultCaseBody) {
				caseState.AddExpression(
						switchCases.Count > 0
								? Expression.Switch(typeof(void),
										switchValue,
										defaultCaseBody,
										node.Comparison,
										switchCases
												.GroupSame(sc => sc.targetState, sc => sc.testValue, ReferenceEqualityComparer<AsyncState>.Default)
												.Select(g => Expression.SwitchCase(
														g.Key.ToExpression(builder.VarState),
														g)))
								: defaultCaseBody);
				switchCases.Clear();
				caseState.OmitStateAssignment();
			}

			foreach (var (testValue, body) in cases) {
				if (testValue.IsAsync) {
					FlushSwitchCases(testValue.EntryState.ToExpression(builder.VarState));
					caseState = testValue.ExitState;
				}
				switchCases.Add((testValue.Expression, caseBodies[body].EntryState));
			}
			FlushSwitchCases(Expression.Assign(
					builder.VarState,
					Expression.Constant(defaultBody.EntryState.StateId)));
			return currentState.ResultExpression;
		}
	}
}
