using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace bsn.AsyncLambdaExpression.Enumerable {
	public class AsyncEnumerableSource<T>: IAsyncEnumerable<T> {
		public delegate ValueTask<bool> MoveNextFunc(bool dispose, StrongBox<T> current);

		private readonly Func<CancellationToken, MoveNextFunc> getEnumeratorFunc;

		public AsyncEnumerableSource(Func<CancellationToken, MoveNextFunc> getEnumeratorFunc) {
			this.getEnumeratorFunc = getEnumeratorFunc;
		}

		private class AsyncEnumerator: StrongBox<T>, IAsyncEnumerator<T> {
			private readonly MoveNextFunc moveNextFunc;

			public AsyncEnumerator(MoveNextFunc moveNextFunc) {
				this.moveNextFunc = moveNextFunc;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public ValueTask DisposeAsync() {
				return new ValueTask(this.moveNextFunc(true, this).AsTask());
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
