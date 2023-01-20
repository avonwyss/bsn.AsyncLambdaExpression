using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace bsn.AsyncLambdaExpression.Collections {
	public class TypeAssignableSet: ICollection<Type> {
		private readonly List<Type> types = new();

		void ICollection<Type>.Add(Type item) {
			this.Add(item);
		}

		public void Clear() {
			this.types.Clear();
		}

		public bool Contains(Type type) {
			return this.types.Any(t => t.IsAssignableFrom(type));
		}

		void ICollection<Type>.CopyTo(Type[] array, int arrayIndex) {
			this.types.CopyTo(array, arrayIndex);
		}

		bool ICollection<Type>.Remove(Type item) {
			throw new NotSupportedException("Removing is not supported");
		}

		public int Count => this.types.Count;

		bool ICollection<Type>.IsReadOnly => false;

		public bool Add(Type type) {
			if (type == null) {
				return false;
			}
			if (this.Contains(type)) {
				return false;
			}
			this.types.RemoveAll(type.IsAssignableFrom);
			this.types.Add(type);
			return true;
		}

		public IEnumerator<Type> GetEnumerator() {
			return this.types.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return this.types.GetEnumerator();
		}
	}
}
