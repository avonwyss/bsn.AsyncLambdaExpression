using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace bsn.AsyncLambdaExpression.Enumerable {
	internal static class EnumerableSourceEmitterFactory {
		public static IEnumerableSourceEmitter Get(Type returnType) {
			if (returnType.IsGenericType) {
				if (returnType.GetGenericTypeDefinition() == typeof(IEnumerable<>)) {
					return (IEnumerableSourceEmitter)Activator.CreateInstance(typeof(EnumerableSourceEmitter<>).MakeGenericType(returnType.GetGenericArguments()));
				}
				if (returnType.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>)) {
					return (IEnumerableSourceEmitter)Activator.CreateInstance(typeof(AsyncEnumerableSourceEmitter<>).MakeGenericType(returnType.GetGenericArguments()));
				}
			}
			throw new InvalidOperationException($"There is no EnumerableSource for {returnType.Name}");
		}
	}
}
