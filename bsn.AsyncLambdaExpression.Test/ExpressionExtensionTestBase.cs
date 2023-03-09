using System;
using System.Linq.Expressions;
using System.Reflection;

using Xunit.Abstractions;

namespace bsn.AsyncLambdaExpression {
	public abstract class ExpressionExtensionTestBase {
		private static readonly ConstructorInfo ctor_InvalidOperationExpression = typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) });

		protected ExpressionExtensionTestBase(ITestOutputHelper output) {
			this.Output = output;
		}

		protected ITestOutputHelper Output {
			get;
		}

		protected static UnaryExpression Throw<T>(string message = "This branch should not be reached during execution") {
			return Expression.Throw(
					Expression.New(ctor_InvalidOperationExpression,
							Expression.Constant(message)),
					typeof(T));
		}
	}
}
