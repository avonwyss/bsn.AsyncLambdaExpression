using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using bsn.AsyncLambdaExpression.Enumerable;

using JetBrains.Annotations;

namespace bsn.AsyncLambdaExpression {
	public static class IteratorExpressionExtensions {
		private const string MessageNotConvertedToYieldStateMachine = "The lambda expression was not converted to an yield state machine";

		// ReSharper disable InconsistentNaming
		private static readonly MethodInfo meth_YieldReturn = typeof(IteratorExpressionExtensions)
				.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly)
				.Single(m => m.Name == nameof(YieldReturn) && m.ReturnType == typeof(void) && m.IsGenericMethod);
		// ReSharper restore InconsistentNaming

		public static bool IsYieldReturnMethod(MethodInfo method) {
			if (!method.IsGenericMethod) {
				return false;
			}
			var methodDefinition = method.GetGenericMethodDefinition();
			return methodDefinition == meth_YieldReturn;
		}

		// ReSharper disable once UnusedParameter.Local
		private static void YieldReturn<T>(T item) {
			throw new NotSupportedException(MessageNotConvertedToYieldStateMachine);
		}

		public static Expression YieldReturn([NotNull] this Expression expression) {
			if (expression == null) {
				throw new ArgumentNullException(nameof(expression));
			}
			return Expression.Call(meth_YieldReturn.MakeGenericMethod(expression.Type), expression);
		}

		public static Expression<Func<IEnumerable<TResult>>> Enumerable<TResult>(this Expression<Action> expression, bool debug = false) {
			return Expression.Lambda<Func<IEnumerable<TResult>>>(new IteratorStateMachineBuilder(expression, typeof(IEnumerable<TResult>)).CreateStateMachineBody(debug), expression.Parameters);
		}

		public static Expression<Func<T, IEnumerable<TResult>>> Enumerable<T, TResult>(this Expression<Action<T>> expression, bool debug = false) {
			return Expression.Lambda<Func<T, IEnumerable<TResult>>>(new IteratorStateMachineBuilder(expression, typeof(IEnumerable<TResult>)).CreateStateMachineBody(debug), expression.Parameters);
		}

		public static Expression<Func<T1, T2, IEnumerable<TResult>>> Enumerable<T1, T2, TResult>(this Expression<Action<T1, T2>> expression, bool debug = false) {
			return Expression.Lambda<Func<T1, T2, IEnumerable<TResult>>>(new IteratorStateMachineBuilder(expression, typeof(IEnumerable<TResult>)).CreateStateMachineBody(debug), expression.Parameters);
		}

		public static Expression<Func<T1, T2, T3, IEnumerable<TResult>>> Enumerable<T1, T2, T3, TResult>(this Expression<Action<T1, T2, T3>> expression, bool debug = false) {
			return Expression.Lambda<Func<T1, T2, T3, IEnumerable<TResult>>>(new IteratorStateMachineBuilder(expression, typeof(IEnumerable<TResult>)).CreateStateMachineBody(debug), expression.Parameters);
		}

		public static Expression<Func<T1, T2, T3, T4, IEnumerable<TResult>>> Enumerable<T1, T2, T3, T4, TResult>(this Expression<Action<T1, T2, T3, T4>> expression, bool debug = false) {
			return Expression.Lambda<Func<T1, T2, T3, T4, IEnumerable<TResult>>>(new IteratorStateMachineBuilder(expression, typeof(IEnumerable<TResult>)).CreateStateMachineBody(debug), expression.Parameters);
		}

		public static Expression<Func<T1, T2, T3, T4, T5, IEnumerable<TResult>>> Enumerable<T1, T2, T3, T4, T5, TResult>(this Expression<Action<T1, T2, T3, T4, T5>> expression, bool debug = false) {
			return Expression.Lambda<Func<T1, T2, T3, T4, T5, IEnumerable<TResult>>>(new IteratorStateMachineBuilder(expression, typeof(IEnumerable<TResult>)).CreateStateMachineBody(debug), expression.Parameters);
		}

		public static Expression<Func<T1, T2, T3, T4, T5, T6, IEnumerable<TResult>>> Enumerable<T1, T2, T3, T4, T5, T6, TResult>(this Expression<Action<T1, T2, T3, T4, T5, T6>> expression, bool debug = false) {
			return Expression.Lambda<Func<T1, T2, T3, T4, T5, T6, IEnumerable<TResult>>>(new IteratorStateMachineBuilder(expression, typeof(IEnumerable<TResult>)).CreateStateMachineBody(debug), expression.Parameters);
		}

		public static Expression<Func<T1, T2, T3, T4, T5, T6, T7, IEnumerable<TResult>>> Enumerable<T1, T2, T3, T4, T5, T6, T7, TResult>(this Expression<Action<T1, T2, T3, T4, T5, T6, T7>> expression, bool debug = false) {
			return Expression.Lambda<Func<T1, T2, T3, T4, T5, T6, T7, IEnumerable<TResult>>>(new IteratorStateMachineBuilder(expression, typeof(IEnumerable<TResult>)).CreateStateMachineBody(debug), expression.Parameters);
		}

		public static Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, IEnumerable<TResult>>> Enumerable<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(this Expression<Action<T1, T2, T3, T4, T5, T6, T7, T8>> expression, bool debug = false) {
			return Expression.Lambda<Func<T1, T2, T3, T4, T5, T6, T7, T8, IEnumerable<TResult>>>(new IteratorStateMachineBuilder(expression, typeof(IEnumerable<TResult>)).CreateStateMachineBody(debug), expression.Parameters);
		}

		public static Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, IEnumerable<TResult>>> Enumerable<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(this Expression<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9>> expression, bool debug = false) {
			return Expression.Lambda<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, IEnumerable<TResult>>>(new IteratorStateMachineBuilder(expression, typeof(IEnumerable<TResult>)).CreateStateMachineBody(debug), expression.Parameters);
		}

		public static Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, IEnumerable<TResult>>> Enumerable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(this Expression<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>> expression, bool debug = false) {
			return Expression.Lambda<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, IEnumerable<TResult>>>(new IteratorStateMachineBuilder(expression, typeof(IEnumerable<TResult>)).CreateStateMachineBody(debug), expression.Parameters);
		}

		public static Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, IEnumerable<TResult>>> Enumerable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(this Expression<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>> expression, bool debug = false) {
			return Expression.Lambda<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, IEnumerable<TResult>>>(new IteratorStateMachineBuilder(expression, typeof(IEnumerable<TResult>)).CreateStateMachineBody(debug), expression.Parameters);
		}

		public static Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, IEnumerable<TResult>>> Enumerable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(this Expression<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>> expression, bool debug = false) {
			return Expression.Lambda<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, IEnumerable<TResult>>>(new IteratorStateMachineBuilder(expression, typeof(IEnumerable<TResult>)).CreateStateMachineBody(debug), expression.Parameters);
		}

		public static Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, IEnumerable<TResult>>> Enumerable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(this Expression<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>> expression,
				bool debug = false) {
			return Expression.Lambda<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, IEnumerable<TResult>>>(new IteratorStateMachineBuilder(expression, typeof(IEnumerable<TResult>)).CreateStateMachineBody(debug), expression.Parameters);
		}

		public static Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, IEnumerable<TResult>>> Enumerable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(this Expression<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>> expression,
				bool debug = false) {
			return Expression.Lambda<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, IEnumerable<TResult>>>(new IteratorStateMachineBuilder(expression, typeof(IEnumerable<TResult>)).CreateStateMachineBody(debug), expression.Parameters);
		}

		public static Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, IEnumerable<TResult>>> Enumerable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>(
				this Expression<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>> expression, bool debug = false) {
			return Expression.Lambda<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, IEnumerable<TResult>>>(new IteratorStateMachineBuilder(expression, typeof(IEnumerable<TResult>)).CreateStateMachineBody(debug), expression.Parameters);
		}

		public static Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, IEnumerable<TResult>>> Enumerable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult>(
				this Expression<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>> expression, bool debug = false) {
			return Expression.Lambda<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, IEnumerable<TResult>>>(new IteratorStateMachineBuilder(expression, typeof(IEnumerable<TResult>)).CreateStateMachineBody(debug), expression.Parameters);
		}

		public static LambdaExpression Enumerable(this LambdaExpression expression, Type delegateType, bool debug = false) {
			return Expression.Lambda(delegateType, new IteratorStateMachineBuilder(expression, delegateType.GetDelegateInvokeMethod().ReturnType).CreateStateMachineBody(debug), expression.Parameters);
		}
	}
}
