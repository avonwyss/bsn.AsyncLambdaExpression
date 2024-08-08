using System;
using System.Linq.Expressions;
using System.Reflection;

using bsn.AsyncLambdaExpression.Expressions;

namespace bsn.AsyncLambdaExpression {
	internal class StubStateMachineExpressions: ExpressionVisitor, IAsyncExpressionVisitor, IIteratorExpressionVisitor {
		private static readonly MethodInfo meth_AwaitVoid = typeof(StubStateMachineExpressions).GetMethod(nameof(AwaitVoid), BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static);
		private static readonly MethodInfo meth_AwaitResult = typeof(StubStateMachineExpressions).GetMethod(nameof(AwaitResult), BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static);
		private static readonly MethodInfo meth_YieldReturn = typeof(StubStateMachineExpressions).GetMethod(nameof(YieldReturn), BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static);

		public static Expression<TDelegate> Process<TDelegate>(StateMachineLambdaExpression<TDelegate> lambda) where TDelegate: Delegate {
			return (Expression<TDelegate>)new StubStateMachineExpressions().Visit(lambda);
		}

		// ReSharper disable once UnusedParameter.Global
		public static void AwaitVoid<TAwaitable>(TAwaitable awaitable) {
			throw new InvalidOperationException("Not a state machine");
		}
		
		// ReSharper disable once UnusedParameter.Global
		public static TResult AwaitResult<TAwaitable, TResult>(TAwaitable awaitable) {
			throw new InvalidOperationException("Not a state machine");
		}
		
		// ReSharper disable once UnusedParameter.Global
		public static void YieldReturn<T>(T item) {
			throw new InvalidOperationException("Not a state machine");
		}
		
		public Expression VisitAsyncLambda<TDelegate>(AsyncLambdaExpression<TDelegate> node) where TDelegate: Delegate {
			return Expression.Lambda(
					Tasks.CompletionSourceEmitterFactory.GetFromResult(typeof(TDelegate).GetDelegateInvokeMethod().ReturnType, this.Visit(node.Body)),
					node.Name,
					node.Parameters);
		}

		public Expression VisitAwait(AwaitExpression node) {
			return Expression.Call(node.Type == typeof(void) ? meth_AwaitVoid.MakeGenericMethod(node.Awaitable.Type) : meth_AwaitResult.MakeGenericMethod(node.Awaitable.Type, node.Type), this.Visit(node.Awaitable));
		}

		public Expression VisitIteratorLambda<TDelegate>(IteratorLambdaExpression<TDelegate> node) where TDelegate: Delegate {
			return Expression.Lambda<TDelegate>(
					Expression.Block(
							this.Visit(node.Body),
							Expression.Default(typeof(TDelegate).GetDelegateInvokeMethod().ReturnType)),
					node.Name,
					node.Parameters);
		}

		public Expression VisitYieldReturn(YieldReturnExpression node) {
			return Expression.Call(meth_YieldReturn.MakeGenericMethod(node.Yield.Type), this.Visit(node.Yield));
		}
	}
}
