using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace bsn.AsyncLambdaExpression.Enumerable {
	public class EnumerableSource<T>: IEnumerable<T> {
		private readonly Func<Func<bool, StrongBox<T>, bool>> getEnumeratorFunc;

		public EnumerableSource(Func<Func<bool, StrongBox<T>, bool>> getEnumeratorFunc) {
			this.getEnumeratorFunc = getEnumeratorFunc;
		}

		private class Enumerator: StrongBox<T>, IEnumerator<T> {
			private readonly Func<bool, StrongBox<T>, bool> moveNextFunc;

			public Enumerator(Func<bool, StrongBox<T>, bool> moveNextFunc) {
				this.moveNextFunc = moveNextFunc;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Dispose() {
				this.moveNextFunc(true, this);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool MoveNext() {
				return this.moveNextFunc(false, this);
			}

			public void Reset() {
				throw new NotSupportedException();
			}

			public T Current {
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => this.Value;
			}

			object IEnumerator.Current {
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => this.Value;
			}
		}

		public IEnumerator<T> GetEnumerator() {
			return new Enumerator(this.getEnumeratorFunc());
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return this.GetEnumerator();
		}
	}
}
