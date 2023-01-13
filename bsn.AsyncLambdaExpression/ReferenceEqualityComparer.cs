﻿using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace bsn.AsyncLambdaExpression {
	public class ReferenceEqualityComparer<T>: IEqualityComparer<T> where T: class {
		public static readonly ReferenceEqualityComparer<T> Default = new ReferenceEqualityComparer<T>();

		public bool Equals(T x, T y) {
			return ReferenceEquals(x, y);
		}

		public int GetHashCode(T obj) {
			return RuntimeHelpers.GetHashCode(obj);
		}
	}
}
