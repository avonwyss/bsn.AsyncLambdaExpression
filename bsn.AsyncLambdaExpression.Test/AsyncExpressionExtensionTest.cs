using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using ExpressionTreeToString;

using Xunit;
using Xunit.Abstractions;

namespace bsn.AsyncLambdaExpression {
	public class AsyncExpressionExtensionTest {
		private static readonly ConstructorInfo ctor_InvalidOperationExpression = typeof(InvalidOperationException).GetConstructor(new [] {typeof(string)});

		public static UnaryExpression Throw<T>(string message = "This branch should not be reached during execution") =>
				Expression.Throw(
						Expression.New(ctor_InvalidOperationExpression,
								Expression.Constant(message)),
						typeof(T));

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
		public async Task TestCompletedCompiled() {
			var compiled = CompileAsyncLambda<Task<string>, string>(paraInput =>
					paraInput.Await(false));
			var result = await compiled(Task.FromResult("success")).ConfigureAwait(false);
			Assert.Equal("success", result);
		}

		[Fact]
		public async Task TestDelayCompiled() {
			var compiled = CompileAsyncLambda<Task<string>, string>(paraInput =>
					Expression.Block(
							Expression.Call(typeof(Task).GetMethod(nameof(Task.Delay), new[] { typeof(int) }),
											Expression.Constant(50))
									.Await(false),
							paraInput.Await(false)));
			var result = await compiled(Task.FromResult("success")).ConfigureAwait(false);
			Assert.Equal("success", result);
		}

		[Fact]
		public async Task TestBlockCompiled() {
			var compiled = CompileAsyncLambda<Task<string>, string>(paraInput =>
					Expression.Block(
							Expression.Constant(Task.FromResult("not-used")).Await(false),
							paraInput.Await(false)));
			var result = await compiled(Task.FromResult("success")).ConfigureAwait(false);
			Assert.Equal("success", result);
		}

		[Fact]
		public async Task TestBinaryCompiled() {
			var compiled = CompileAsyncLambda<Task<int>, int>(paraInput =>
					Expression.Add(
							Expression.Constant(Task.FromResult(1)).Await(false),
							paraInput.Await(false)));
			var result = await compiled(Task.FromResult(2)).ConfigureAwait(false);
			Assert.Equal(3, result);
		}

		[Fact]
		public async Task TestSwitchCompiled() {
			var compiled = CompileAsyncLambda<Task<int>, int>(paraInput =>
					Expression.Switch(paraInput.Await(false),
							Expression.Constant(Task.FromResult(-1)).Await(false),
							Expression.SwitchCase(
									Throw<Task<int>>().Await(false),
									Expression.Constant(Task.FromResult(1)).Await(false))));
			var result = await compiled(Task.FromResult(2)).ConfigureAwait(false);
			Assert.Equal(-1, result);
		}

		[Fact]
		public async Task TestOrElseCompiled() {
			var compiled = CompileAsyncLambda<Task<bool>, bool>(paraInput =>
					Expression.OrElse(
							paraInput.Await(true),
							Throw<Task<bool>>().Await(false)));
			var result = await compiled(Task.FromResult(false)).ConfigureAwait(false);
			Assert.True(result);
		}

		[Fact]
		public async Task TestAndAlsoCompiled() {
			var compiled = CompileAsyncLambda<Task<bool>, bool>(paraInput =>
					Expression.OrElse(
							paraInput.Await(false),
							Throw<Task<bool>>().Await(false)));
			var result = await compiled(Task.FromResult(true)).ConfigureAwait(false);
			Assert.True(result);
		}

		[Fact]
		public async Task TestConditionalCompiled() {
			var compiled = CompileAsyncLambda<Task<bool>, string>(paraInput =>
					Expression.Condition(
							paraInput.Await(false),
							Expression.Constant(Task.FromResult("success")).Await(false),
							Throw<Task<string>>().Await(false)));
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
