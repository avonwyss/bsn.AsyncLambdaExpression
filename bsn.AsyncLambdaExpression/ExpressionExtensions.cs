using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

using bsn.AsyncLambdaExpression.Expressions;

using JetBrains.Annotations;

namespace bsn.AsyncLambdaExpression {
	public static class ExpressionExtensions {
		private class NoStateMachineVars: IStateMachineVariables {
			public NoStateMachineVars(DebugInfoGenerator debugInfoGenerator) {
				this.DebugInfoGenerator = debugInfoGenerator;
			}

			public DebugInfoGenerator DebugInfoGenerator {
				get;
			}

			ParameterExpression IStateMachineVariables.VarException => throw new NotImplementedException();

			ParameterExpression IStateMachineVariables.VarResumeState => throw new NotImplementedException();

			ParameterExpression IStateMachineVariables.VarState => throw new NotImplementedException();

			LabelTarget IStateMachineVariables.LblBreak => throw new NotImplementedException();

			ParameterExpression IStateMachineVariables.VarContinuation => throw new NotImplementedException();

			ParameterExpression IStateMachineVariables.VarCurrent => throw new NotImplementedException();

			ParameterExpression IStateMachineVariables.GetVarAwaiter(Type awaiterType) {
				throw new NotImplementedException();
			}

			Expression IStateMachineVariables.HandleException(ParameterExpression varException) {
				throw new NotImplementedException();
			}
		}
		
		internal static Expression RescopeVariables(this Expression expression, IEnumerable<ParameterExpression> unmanagedVariablesAndParameters) {
			var finder = new VariableScopeFinder(unmanagedVariablesAndParameters);
			finder.Visit(expression);
			var setter = new VariableScopeSetter(finder.GetBlockVariables(), finder.IsIgnored, finder.IsToRemove);
			return setter.Visit(expression);
		}

		internal static Expression Optimize(this Expression expression) {
			var optimizer = new Optimizer();
			return optimizer.Visit(expression);
		}

		public static bool RequiresStateMachine(this Expression expression) {
			return RequiresStateMachine(expression, false);
		}

		public static T BuildStateMachines<T>(this T node, DebugInfoGenerator debugInfoGenerator = null) where T: Expression {
			return new ContinuationBuilder(new NoStateMachineVars(debugInfoGenerator)).VisitAndConvert(node, nameof(BuildStateMachines));
		}

		public static Expression BuildStateMachines(this Expression node, DebugInfoGenerator debugInfoGenerator = null) {
			return new ContinuationBuilder(new NoStateMachineVars(debugInfoGenerator)).Visit(node);
		}

		internal static bool RequiresStateMachine(this Expression expression, bool labelAndGotoAreAsync) {
			var awaitCallChecker = new StateMachineChecker(labelAndGotoAreAsync);
			awaitCallChecker.Visit(expression);
			return awaitCallChecker.RequiresStateMachine;
		}

		internal static bool IsSafeCode(this Expression expression) {
			var safeCodeChecker = new SafeCodeChecker();
			safeCodeChecker.Visit(expression);
			return !safeCodeChecker.ContainsUnsafeCode;
		}
	}
}
