using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace bsn.AsyncLambdaExpression.Tasks {
	public static class CompletionSourceEmitterFactory {
		internal static ICompletionSourceEmitter Get(Type returnType) {
			if (returnType == typeof(Task)) {
				return new TaskCompletionSourceEmitter();
			}
			if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>)) {
				return (ICompletionSourceEmitter)Activator.CreateInstance(typeof(TaskCompletionSourceEmitter<>).MakeGenericType(returnType.GetGenericArguments()));
			}
			if (returnType == typeof(ValueTask)) {
				return new ValueTaskCompletionSourceEmitter();
			}
			if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>)) {
				return (ICompletionSourceEmitter)Activator.CreateInstance(typeof(ValueTaskCompletionSourceEmitter<>).MakeGenericType(returnType.GetGenericArguments()));
			}
			throw new InvalidOperationException($"There is no CompletionSource for {returnType.Name}");
		}

		public static Expression GetFromResult(Type type, Expression result) {
			return Get(type).GetFromResult(result);
		}
	}
}
