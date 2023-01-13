using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace bsn.AsyncLambdaExpression.Collections {
	public sealed class ImmutableStack<T>: IEnumerable<T> {
		public static readonly ImmutableStack<T> Empty = new ImmutableStack<T>(null, default);

		private readonly ImmutableStack<T> parent;
		private readonly T value;

		private ImmutableStack(ImmutableStack<T> parent, T value) {
			this.parent = parent;
			this.value = value;
		}

		public bool IsEmpty {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => parent == null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void AssertNotEmpty() {
			if (IsEmpty) {
				throw new InvalidOperationException("Stack is empty");
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ImmutableStack<T> Pop() {
			AssertNotEmpty();
			return parent;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Peek() {
			AssertNotEmpty();
			return value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T PeekOrDefault() {
			return value;
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
			return GetEnumerator();
		}
	}
}
