using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

using bsn.AsyncLambdaExpression.Enumerable;
using bsn.AsyncLambdaExpression.Expressions;

namespace bsn.AsyncLambdaExpression {
	internal class IteratorStateMachineBuilder: StateMachineBuilderBase {
		// ReSharper disable InconsistentNaming
		private static readonly ConcurrentDictionary<Type, (ConstructorInfo, MethodInfo)> enumerableSourceInfos = new();
		// ReSharper restore InconsistentNaming

		internal static (ConstructorInfo ctor, MethodInfo meth_GetEnumerator) GetEnumerableSourceInfo(Type type) {
			System.Diagnostics.Debug.Assert(type.GetGenericTypeDefinition() == typeof(EnumerableSource<>));
			return enumerableSourceInfos.GetOrAdd(type, t => (
					t.GetConstructors().Single(c => c.GetParameters().Length == 1 && typeof(Delegate).IsAssignableFrom(c.GetParameters()[0].ParameterType)),
					t.GetMethod(nameof(EnumerableSource<object>.GetEnumerator), BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null)
			));
		}

		public IteratorStateMachineBuilder(StateMachineLambdaExpression lambda, Type returnType, DebugInfoGenerator debugInfoGenerator): base(lambda, typeof(bool), debugInfoGenerator) {
			this.ElementType = returnType.GetEnumerableItemType();
			this.VarCurrent = Expression.Parameter(typeof(StrongBox<>).MakeGenericType(this.ElementType), "current");
		}

		public Type ElementType {
			get;
		}

		public override Expression CreateStateMachineBody() {
			var strongBoxType = typeof(StrongBox<>).MakeGenericType(this.ElementType);
			var varEx = Expression.Variable(typeof(Exception), "ex");
			var continuationBuilder = new ContinuationBuilder(this);
			var (finalState, finalExpr) = continuationBuilder.Process(this.Lambda.Body);
			finalState.AddExpression(finalExpr);
			finalState.AddExpression(Expression.Assign(this.VarState, Expression.Constant(-1)));
			var moveNextType = typeof(Func<,,>).MakeGenericType(typeof(bool), strongBoxType, typeof(bool));
			var paraDispose = Expression.Parameter(typeof(bool), "dispose");
			var moveNextLambda = Expression.Lambda(moveNextType,
					Expression.TryCatch(
							Expression.Loop(
									Expression.Switch(typeof(void), this.VarState,
											Expression.Break(this.LblBreak,
													Expression.Constant(false)),
											null,
											continuationBuilder.States.Select(state =>
													Expression.SwitchCase(
															this.StateBodyExpressionDebug(state, paraDispose),
															Expression.Constant(state.StateId)))), this.LblBreak),
							Expression.Catch(varEx,
									Expression.Block(
											Expression.Assign(this.VarState,
													Expression.Constant(-1)),
											Expression.Rethrow(typeof(bool))))),
					paraDispose, this.VarCurrent);
			var getEnumeratorType = typeof(Func<>).MakeGenericType(moveNextType);
			var variables = this.GetVars().Where(v => v != null).ToList();
			var (ctor, meth_getEnumerator) = GetEnumerableSourceInfo(typeof(EnumerableSource<>).MakeGenericType(this.ElementType));
			var getEnumeratorExpr = Expression.New(ctor,
					Expression.Lambda(getEnumeratorType,
							Expression.Block(variables,
									Expression.Assign(this.VarResumeState, Expression.Constant(-1)),
									moveNextLambda)));
			Expression stateMachine = Expression.Convert(
					this.Lambda.ReturnType.IsEnumerableInterface()
							? getEnumeratorExpr
							: Expression.Call(getEnumeratorExpr, meth_getEnumerator),
					this.Lambda.ReturnType);
			if (this.DebugInfoGenerator == null) {
				stateMachine = stateMachine.Optimize();
			}
			return stateMachine.RescopeVariables(this.Lambda.Parameters.Concat(variables).Append(this.VarCurrent).Append(paraDispose));
		}

		public override Expression HandleException(ParameterExpression varException) {
			return Expression.Throw(varException);
		}
	}
}
