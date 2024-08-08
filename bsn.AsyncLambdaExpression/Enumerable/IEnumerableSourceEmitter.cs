using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace bsn.AsyncLambdaExpression.Enumerable {
	internal interface IEnumerableSourceEmitter {
		Expression Create(Expression getEnumeratorFunc);

		Type GetEnumeratorFuncType {
			get;
		}
	}

	internal class EnumerableSourceEmitter<T>: IEnumerableSourceEmitter {
		private static readonly ConstructorInfo ctor_EnumerableSource = Reflect.GetConstructor(() => new EnumerableSource<T>(default));
		
		public Type GetEnumeratorFuncType {
			get;
		}
		
		public Expression Create(Expression getEnumeratorFunc) {
			return Expression.New(ctor_EnumerableSource);
		}
	}

	internal class AsyncEnumerableSourceEmitter<T>: IEnumerableSourceEmitter {
		public Expression Create(Expression getEnumeratorFunc) {
			throw new NotImplementedException();
		}

		public Type GetEnumeratorFuncType => throw new NotImplementedException();
	}
}
