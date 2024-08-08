using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace bsn.AsyncLambdaExpression.Enumerable {
	public class AsyncEnumerableSource<T>: IAsyncEnumerable<T> {
		private readonly Func<CancellationToken, Func<bool, StrongBox<T>, ValueTask<bool>>> getEnumeratorFunc;

		public AsyncEnumerableSource(Func<CancellationToken, Func<bool, StrongBox<T>, ValueTask<bool>>> getEnumeratorFunc) {
			this.getEnumeratorFunc = getEnumeratorFunc;
		}

		private class AsyncEnumerator: StrongBox<T>, IAsyncEnumerator<T> {
			private readonly Func<bool, StrongBox<T>, ValueTask<bool>> moveNextFunc;

			public AsyncEnumerator(Func<bool, StrongBox<T>, ValueTask<bool>> moveNextFunc) {
				this.moveNextFunc = moveNextFunc;
			}

			public async ValueTask DisposeAsync() {
				await this.moveNextFunc(true, this).ConfigureAwait(false);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public ValueTask<bool> MoveNextAsync() {
				return this.moveNextFunc(false, this);
			}

			public T Current {
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => this.Value;
			}
		}

		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
			return new AsyncEnumerator(this.getEnumeratorFunc(cancellationToken));
		}
	}
}
