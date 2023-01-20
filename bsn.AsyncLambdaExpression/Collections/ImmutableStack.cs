using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace bsn.AsyncLambdaExpression.Collections {
	public sealed class ImmutableStack<T>: IEnumerable<T> {
		public static readonly ImmutableStack<T> Empty = new(null, default);

		private readonly ImmutableStack<T> parent;
		private readonly T value;

		private ImmutableStack(ImmutableStack<T> parent, T value) {
			this.parent = parent;
			this.value = value;
		}

		public bool IsEmpty {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.parent == null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void AssertNotEmpty() {
			if (this.IsEmpty) {
				throw new InvalidOperationException("Stack is empty");
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ImmutableStack<T> Pop() {
			this.AssertNotEmpty();
			return this.parent;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Peek() {
			this.AssertNotEmpty();
			return this.value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T PeekOrDefault() {
			return this.value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ImmutableStack<T> Push(T value) {
			return new ImmutableStack<T>(this, value);
		}

		public IEnumerator<T> GetEnumerator() {
			for (var item = this; item.parent != null; item = item.parent) {
				yield return item.value;
			}
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return this.GetEnumerator();
		}
	}
}
