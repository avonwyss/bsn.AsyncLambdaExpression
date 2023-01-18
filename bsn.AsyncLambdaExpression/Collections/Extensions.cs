using System;
using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

namespace bsn.AsyncLambdaExpression.Collections {
	public static class Extensions {
		private sealed class ListGrouping<TKey, TValue> : List<TValue>, IGrouping<TKey, TValue> {
			public ListGrouping(TKey key) {
				this.Key = key;
			}

			public TKey Key {
				get;
			}
		}

		/// <summary>
		/// Group By, but separate grouping as soon as key changes.
		/// </summary>
		[Pure]
		public static IEnumerable<IGrouping<TKey, TValue>> GroupSame<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> input, IEqualityComparer<TKey> keyComparer = null) {
			return GroupSame(input, p => p.Key, p => p.Value, keyComparer);
		}

		/// <summary>
		/// Group By, but separate grouping as soon as key changes.
		/// </summary>
		[Pure]
		public static IEnumerable<IGrouping<TKey, TInput>> GroupSame<TInput, TKey>(this IEnumerable<TInput> input, Func<TInput, TKey> keySelector, IEqualityComparer<TKey> keyComparer = null) {
			return GroupSame(input, keySelector, v => v, keyComparer);
		}

		/// <summary>
		/// Group By, but separate grouping as soon as key changes.
		/// </summary>
		[Pure]
		public static IEnumerable<IGrouping<TKey, TValue>> GroupSame<TInput, TKey, TValue>(this IEnumerable<TInput> input, Func<TInput, TKey> keySelector, Func<TInput, TValue> valueSelector, IEqualityComparer<TKey> keyComparer = null) {
			keyComparer ??= EqualityComparer<TKey>.Default;
			using var enumerator = input.GetEnumerator();
			if (!enumerator.MoveNext()) {
				yield break;
			}
			var current = new ListGrouping<TKey, TValue>(keySelector(enumerator.Current)) {
					valueSelector(enumerator.Current)
			};
			while (enumerator.MoveNext()) {
				var key = keySelector(enumerator.Current);
				if (!keyComparer.Equals(current.Key, key)) {
					yield return current;
					current = new ListGrouping<TKey, TValue>(key);
				}
				current.Add(valueSelector(enumerator.Current));
			}
			yield return current;
		}
	}
}
