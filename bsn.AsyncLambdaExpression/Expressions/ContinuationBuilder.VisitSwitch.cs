using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using bsn.AsyncLambdaExpression.Collections;

namespace bsn.AsyncLambdaExpression.Expressions {
	internal partial class ContinuationBuilder {
		protected override Expression VisitSwitch(SwitchExpression node) {
			var switchValue = Visit(node.SwitchValue);
			var switchValueExitState = currentState;
			var cases = node
					.Cases
					.SelectMany(sc => sc.TestValues.Select<Expression, (Fiber testValue, Expression body)>(tv => {
						var fiber = VisitAsFiber(tv, false);
						fiber.SetName("Switch", switchValueExitState.StateId, "Test Value");
						return (fiber, sc.Body);
					}))
					.ToArray();
			var hasAsyncTestValues = cases.Any(c => c.testValue.IsAsync);
			var caseBodies = cases
					.Select(c => c.body)
					.Distinct()
					.ToDictionary(b => b, b => {
						var fiber = VisitAsFiber(b, hasAsyncTestValues);
						fiber.SetName("Switch", switchValueExitState.StateId, "Case Body");
						return fiber;
					});
			var defaultBody = VisitAsFiber(node.DefaultBody, hasAsyncTestValues);
			defaultBody.SetName("Switch", switchValueExitState.StateId, "Default");
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
			currentState.SetName("Switch", switchValueExitState.StateId, "Merge");
			defaultBody.ContinueWith(currentState);
			foreach (var caseBodyValue in caseBodies.Values) {
				caseBodyValue.ContinueWith(currentState);
			}
			if (!hasAsyncTestValues) {
				// No async test values, emit a void switch-case and merge the result in a distinct state
				caseState.AddExpression(
						Expression.Switch(typeof(void), switchValue, defaultBody.EntryState.ToExpression(vars), node.Comparison, cases
								.GroupSame(c => c.body, c => c.testValue, ReferenceEqualityComparer<Expression>.Default)
								.Select(g => Expression.SwitchCase(caseBodies[g.Key].EntryState.ToExpression(vars), g.Select(f => f.Expression)))));
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
														g.Key.ToExpression(vars),
														g)))
								: defaultCaseBody);
				switchCases.Clear();
			}

			foreach (var (testValue, body) in cases) {
				if (testValue.IsAsync) {
					FlushSwitchCases(testValue.EntryState.ToExpression(vars));
					caseState = testValue.ExitState;
				}
				switchCases.Add((testValue.Expression, caseBodies[body].EntryState));
			}
			FlushSwitchCases(Expression.Assign(
					vars.VarState,
					Expression.Constant(defaultBody.EntryState.StateId)));
			return currentState.ResultExpression;
		}
	}
}
