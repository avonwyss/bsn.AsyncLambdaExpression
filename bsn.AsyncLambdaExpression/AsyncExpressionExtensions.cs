using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using JetBrains.Annotations;

using Seram.Web;

namespace bsn.AsyncLambdaExpression {
	public static class AsyncExpressionExtensions {
		private const string MessageNotConvertedToAsyncStateMachine = "The lambda expression was not converted to an async state machine";

		private static readonly MethodInfo meth_AwaitPlaceholderResult = typeof(AsyncExpressionExtensions).GetMethod(nameof(AwaitPlaceholderResult), BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.DeclaredOnly);
		private static readonly MethodInfo meth_AwaitPlaceholderVoid = typeof(AsyncExpressionExtensions).GetMethod(nameof(AwaitPlaceholderVoid), BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.DeclaredOnly);

		public static bool IsAwaitMethod(MethodInfo method) {
			if (!method.IsGenericMethod) {
				return false;
			}
			var methodDefinition = method.GetGenericMethodDefinition();
			return methodDefinition == meth_AwaitPlaceholderResult || methodDefinition == meth_AwaitPlaceholderVoid;
		}

		// ReSharper disable once UnusedParameter.Local
		private static TResult AwaitPlaceholderResult<TAwaitable, TResult>(TAwaitable awaitable) {
			throw new NotSupportedException(MessageNotConvertedToAsyncStateMachine);
		}

		private static void AwaitPlaceholderVoid<TAwaitable>(TAwaitable awaitable) {
			throw new NotSupportedException(MessageNotConvertedToAsyncStateMachine);
		}

		public static Expression Await([NotNull] this Expression expression, bool? continueOnCapturedContext = null) {
			if (expression == null) {
				throw new ArgumentNullException(nameof(expression));
			}
			if (continueOnCapturedContext.HasValue) {
				var meth_ConfigureAwait = expression.Type.GetAwaitableGetConfigureAwaitMethod();
				if (meth_ConfigureAwait == null) {
					throw new ArgumentException($"The type {expression.Type.FullName} of the expression does not have a ConfigureAwait(bool) method", nameof(expression));
				}
				expression = Expression.Call(expression, meth_ConfigureAwait, Expression.Constant(continueOnCapturedContext.Value));
			}
			var methGetAwaiter = expression.Type.GetAwaitableGetAwaiterMethod();
			if (methGetAwaiter == null) {
				throw new ArgumentException($"The type {expression.Type.FullName} of the expression is not awaitable", nameof(expression));
			}
			var methGetResult = methGetAwaiter.ReturnType.GetAwaiterGetResultMethod();
			return Expression.Call(methGetResult.ReturnType == typeof(void)
					? meth_AwaitPlaceholderVoid.MakeGenericMethod(expression.Type)
					: meth_AwaitPlaceholderResult.MakeGenericMethod(expression.Type, methGetResult.ReturnType), expression);
		}

		public static Expression<T> Async<T>(this LambdaExpression expression) {
			return Expression.Lambda<T>(new AsyncStateMachineBuilder(expression, typeof(T).GetDelegateInvokeMethod().ReturnType).CreateStateMachineBody(), expression.Parameters);
		}

		public static LambdaExpression Async(this LambdaExpression expression, Type delegateType) {
			return Expression.Lambda(delegateType, new AsyncStateMachineBuilder(expression, delegateType.GetDelegateInvokeMethod().ReturnType).CreateStateMachineBody(), expression.Parameters);
		}
	}
}
