using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

using JetBrains.Annotations;

namespace bsn.AsyncLambdaExpression.Expressions {
	public static class IteratorExpression {
		public static YieldReturnExpression YieldReturn([NotNull] Expression node) {
			return new YieldReturnExpression(node ?? throw new ArgumentNullException(nameof(node)));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IteratorLambdaExpression<TDelegate> IteratorLambda<TDelegate>(Expression body, params ParameterExpression[] parameters) where TDelegate: Delegate {
			return IteratorLambda<TDelegate>(body, default(string), (IEnumerable<ParameterExpression>)parameters);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IteratorLambdaExpression<TDelegate> IteratorLambda<TDelegate>(Expression body, IEnumerable<ParameterExpression> parameters) where TDelegate: Delegate {
			return IteratorLambda<TDelegate>(body, default(string), (IEnumerable<ParameterExpression>)parameters);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IteratorLambdaExpression<TDelegate> IteratorLambda<TDelegate>(Expression body, string name, params ParameterExpression[] parameters) where TDelegate: Delegate {
			return IteratorLambda<TDelegate>(body, name, (IEnumerable<ParameterExpression>)parameters);
		}

		public static IteratorLambdaExpression<TDelegate> IteratorLambda<TDelegate>(Expression body, string name, IEnumerable<ParameterExpression> parameters) where TDelegate: Delegate {
			var returnType = typeof(TDelegate).GetDelegateInvokeMethod().ReturnType;
			if (!(returnType.IsGenericEnumerableInterface() || returnType.IsEnumerableInterface())) {
				throw new InvalidOperationException($"The delegate {typeof(TDelegate).FullName} does not return an IEnumerable or IEnumerable<>");
			}
			// Using the Expression.Lambda<> in order to perform type and parameter checks
			var lambda = Expression.Lambda<TDelegate>(Expression.Default(returnType), name, parameters);
			// The IteratorLambdaExpression<> constructor does not perform checks
			return new IteratorLambdaExpression<TDelegate>(name, body ?? throw new ArgumentNullException(nameof(body)), lambda.Parameters);
		}
	}
}
