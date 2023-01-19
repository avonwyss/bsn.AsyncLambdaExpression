using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace bsn.AsyncLambdaExpression.Collections {
	public class TypeAssignableSet: ICollection<Type> {
		private readonly List<Type> types = new List<Type>();

		void ICollection<Type>.Add(Type item) {
			Add(item);
		}

		public void Clear() {
			types.Clear();
		}

		public bool Contains(Type type) {
			return types.Any(t => t.IsAssignableFrom(type));
		}

		void ICollection<Type>.CopyTo(Type[] array, int arrayIndex) {
			types.CopyTo(array, arrayIndex);
		}

		bool ICollection<Type>.Remove(Type item) {
			throw new NotSupportedException("Removing is not supported");
		}

		public int Count => types.Count;

		bool ICollection<Type>.IsReadOnly => false;

		public bool Add(Type type) {
			if (type == null) {
				return false;
			}
			if (Contains(type)) {
				return false;
			}
			types.RemoveAll(type.IsAssignableFrom);
			types.Add(type);
			return true;
		}

		public IEnumerator<Type> GetEnumerator() {
			return types.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return types.GetEnumerator();
		}
	}
}
