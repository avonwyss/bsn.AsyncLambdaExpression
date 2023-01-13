using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using ExpressionTreeToString;

using Xunit;
using Xunit.Abstractions;

namespace bsn.AsyncLambdaExpression {
	public class AsyncExpressionExtensionTest {
		public ITestOutputHelper Output {
			get;
		}

		public AsyncExpressionExtensionTest(ITestOutputHelper output) {
			Output = output;
		}

		private Func<TInput, Task<TResult>> CompileAsyncLambda<TInput, TResult>(Func<ParameterExpression, Expression> bodyFactory) {
			var paraInput = Expression.Parameter(typeof(TInput), "input");
			var exprLambda = Expression.Lambda<Func<TInput, TResult>>(bodyFactory(paraInput), paraInput);
			var exprAsync = exprLambda.Async<Func<TInput, Task<TResult>>>();
			Output.WriteLine(exprAsync.ToString(BuiltinRenderer.DebugView));
			return exprAsync.Compile(false);
		}

		[Fact]
		public async Task TestAsyncCompletedCompiled() {
			var compiled = CompileAsyncLambda<Task<string>, string>(paraInput =>
					paraInput.Await(false));
			var result = await compiled(Task.FromResult("success")).ConfigureAwait(false);
			Assert.Equal("success", result);
		}

		[Fact]
		public async Task TestAsyncDelayCompiled() {
			var compiled = CompileAsyncLambda<Task<string>, string>(paraInput =>
					Expression.Block(
							Expression.Call(typeof(Task).GetMethod(nameof(Task.Delay), new[] { typeof(int) }),
											Expression.Constant(5000))
									.Await(false),
							paraInput.Await(false)));
			var result = await compiled(Task.FromResult("success")).ConfigureAwait(false);
			Assert.Equal("success", result);
		}

		[Fact]
		public async Task TestAsyncConditionalCompiled() {
			var compiled = CompileAsyncLambda<Task<bool>, string>(paraInput =>
					Expression.Condition(
							paraInput.Await(false),
							Expression.Constant(Task.FromResult("success")).Await(false),
							Expression.Constant(Task.FromResult("error")).Await(false)));
			var result = await compiled(Task.FromResult(true)).ConfigureAwait(false);
			Assert.Equal("success", result);
		}

		[Fact]
		public void TestInvalidAwait() {
			Assert.Throws<ArgumentException>(() => Expression.Constant("test").Await());
		}

		[Fact]
		public void TestInvalidConfigureAwait() {
			Assert.Throws<ArgumentException>(() => Expression.Constant(Task.Yield()).Await(false));
		}

		[Fact]
		public void TestAwaitType() {
			var exprAwait = Expression.Constant(Task.FromResult("test")).Await();
			Output.WriteLine(exprAwait.ToString(BuiltinRenderer.DebugView));
			Assert.Equal(typeof(string), exprAwait.Type);
		}

		[Fact]
		public void TestAwaitConfiguredType() {
			var exprAwait = Expression.Constant(Task.FromResult("test")).Await(false);
			Output.WriteLine(exprAwait.ToString(BuiltinRenderer.DebugView));
			Assert.Equal(typeof(string), exprAwait.Type);
		}
	}
}
