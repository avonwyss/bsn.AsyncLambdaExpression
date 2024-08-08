using System;
using System.CodeDom;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using bsn.AsyncLambdaExpression.Tasks;

using JetBrains.Annotations;

namespace bsn.AsyncLambdaExpression.Expressions {
	public static class AsyncExpression {
		private static readonly MethodInfo meth_AsyncLambdaOfTDelegate = Reflect.GetStaticMethod(() => AsyncLambda<Delegate>(default(Expression), default(string), default(IEnumerable<ParameterExpression>))).GetGenericMethodDefinition();

		private static MethodInfo Get_Meth_AsyncLambda([NotNull] Type delegateType) {
			return meth_AsyncLambdaOfTDelegate.MakeGenericMethod(delegateType ?? throw new ArgumentNullException(nameof(delegateType)));
		}

		public static StateMachineLambdaExpression AsyncLambda([NotNull] Type delegateType, [NotNull] Expression body, IEnumerable<ParameterExpression> parameters) {
			return (StateMachineLambdaExpression)Get_Meth_AsyncLambda(delegateType).Invoke(null, new object[] {body, null, parameters});
		}

		public static StateMachineLambdaExpression AsyncLambda([NotNull] Type delegateType, [NotNull] Expression body, params ParameterExpression[] parameters) {
			return (StateMachineLambdaExpression)Get_Meth_AsyncLambda(delegateType).Invoke(null, new object[] {body, null, parameters});
		}

		public static StateMachineLambdaExpression AsyncLambda([NotNull] Type delegateType, [NotNull] Expression body, string name, params ParameterExpression[] parameters) {
			return (StateMachineLambdaExpression)Get_Meth_AsyncLambda(delegateType).Invoke(null, new object[] {body, name, parameters});
		}

		public static StateMachineLambdaExpression AsyncLambda([NotNull] Type delegateType, [NotNull] Expression body, string name, IEnumerable<ParameterExpression> parameters) {
			return (StateMachineLambdaExpression)Get_Meth_AsyncLambda(delegateType).Invoke(null, new object[] {body, name, parameters});
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static AsyncLambdaExpression<TDelegate> AsyncLambda<TDelegate>([NotNull] Expression body, IEnumerable<ParameterExpression> parameters) where TDelegate: Delegate {
			return AsyncLambda<TDelegate>(body, default(string), parameters);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static AsyncLambdaExpression<TDelegate> AsyncLambda<TDelegate>([NotNull] Expression body, params ParameterExpression[] parameters) where TDelegate: Delegate {
			return AsyncLambda<TDelegate>(body, default(string), (IEnumerable<ParameterExpression>)parameters);
		}

		public static AsyncLambdaExpression<TDelegate> AsyncLambda<TDelegate>([NotNull] Expression body, string name, IEnumerable<ParameterExpression> parameters) where TDelegate: Delegate {
			// Using the Emitter.GetFromResult to wraps the body into a result, thereby making sure that the method signature is correct
			var emitter = CompletionSourceEmitterFactory.Get(typeof(TDelegate).GetDelegateInvokeMethod().ReturnType);
			// Using the Expression.Lambda<> in order to perform type and parameter checks
			var lambda = Expression.Lambda<TDelegate>(emitter.GetFromResult(body ?? throw new ArgumentNullException(nameof(body))), name, parameters);
			// The AsyncLambdaExpression<> constructor does not perform checks
			return new AsyncLambdaExpression<TDelegate>(name, body, lambda.Parameters);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static AsyncLambdaExpression<TDelegate> AsyncLambda<TDelegate>([NotNull] Expression body, string name, params ParameterExpression[] parameters) where TDelegate: Delegate {
			return AsyncLambda<TDelegate>(body, name, (IEnumerable<ParameterExpression>)parameters);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Expression AwaitIfAwaitable([NotNull] Expression maybeAwaitable) {
			return maybeAwaitable.Type.IsAwaitable()
					? Await(maybeAwaitable)
					: maybeAwaitable;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Expression AwaitIfAwaitableConfigured([NotNull] Expression maybeAwaitable, bool continueOnCapturedContext) {
			return maybeAwaitable.Type.IsAwaitable()
					? AwaitConfigured(maybeAwaitable, continueOnCapturedContext)
					: maybeAwaitable;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Expression AwaitIfAwaitableConfiguredOptional([NotNull] Expression maybeAwaitable, bool continueOnCapturedContext) {
			return maybeAwaitable.Type.IsAwaitable()
					? AwaitConfiguredOptional(maybeAwaitable, continueOnCapturedContext)
					: maybeAwaitable;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static AwaitExpression Await([NotNull] Expression awaitable) {
			return new AwaitExpression(awaitable);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static AwaitExpression AwaitConfigured([NotNull] Expression awaitable, bool continueOnCapturedContext) {
			return new AwaitExpression(ConfigureAwaitInternal(awaitable, Expression.Constant(continueOnCapturedContext, typeof(bool)), false));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static AwaitExpression AwaitConfiguredOptional([NotNull] Expression awaitable, bool continueOnCapturedContext) {
			return new AwaitExpression(ConfigureAwaitInternal(awaitable, Expression.Constant(continueOnCapturedContext, typeof(bool)), true));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static MethodCallExpression ConfigureAwait([NotNull] Expression awaitable, bool continueOnCapturedContext) {
			return (MethodCallExpression)ConfigureAwaitInternal(awaitable, Expression.Constant(continueOnCapturedContext, typeof(bool)), false);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static MethodCallExpression ConfigureAwait([NotNull] Expression awaitable, [NotNull] Expression continueOnCapturedContext) {
			return (MethodCallExpression)ConfigureAwaitInternal(awaitable, continueOnCapturedContext, false);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Expression ConfigureAwaitOptional([NotNull] Expression awaitable, bool continueOnCapturedContext) {
			return ConfigureAwaitInternal(awaitable, Expression.Constant(continueOnCapturedContext, typeof(bool)), true);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Expression ConfigureAwaitOptional([NotNull] Expression awaitable, [NotNull] Expression continueOnCapturedContext) {
			return ConfigureAwaitInternal(awaitable, continueOnCapturedContext, true);
		}

		private static Expression ConfigureAwaitInternal([NotNull] Expression awaitable, [NotNull] Expression continueOnCapturedContext, bool ignoreMissingConfigureAwaitMethod) {
			var meth_ConfigureAwait = (awaitable ?? throw new ArgumentNullException(nameof(awaitable))).Type.GetAwaitableGetConfigureAwaitMethod();
			return meth_ConfigureAwait != null
					? Expression.Call(awaitable, meth_ConfigureAwait, continueOnCapturedContext ?? throw new ArgumentNullException(nameof(continueOnCapturedContext)))
					: ignoreMissingConfigureAwaitMethod
							? awaitable
							: throw new ArgumentException($"The type {awaitable.Type} does not have a ConfigureAwait(bool) method");
		}
	}
}
