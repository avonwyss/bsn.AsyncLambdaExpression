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
		private static readonly ConstructorInfo ctor_InvalidOperationExpression = typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) });

		public static UnaryExpression Throw<T>(string message = "This branch should not be reached during execution") {
			return Expression.Throw(
					Expression.New(ctor_InvalidOperationExpression,
							Expression.Constant(message)),
					typeof(T));
		}

		public ITestOutputHelper Output {
			get;
		}

		public AsyncExpressionExtensionTest(ITestOutputHelper output) {
			Output = output;
		}

		private Func<TInput, Task<TResult>> CompileAsyncLambda<TInput, TResult>(Func<ParameterExpression, Expression> bodyFactory) {
			var paraInput = Expression.Parameter(typeof(TInput), "input");
			var exprLambda = Expression.Lambda<Func<TInput, TResult>>(bodyFactory(paraInput), paraInput);
			Output.WriteLine("==> Original Lambda");
			Output.WriteLine(exprLambda.ToString(BuiltinRenderer.DebugView));
			var exprAsync = exprLambda.Async<Func<TInput, Task<TResult>>>();
			Output.WriteLine("");
			Output.WriteLine("==> Async Lambda");
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
		public async Task TestSyncCompiled() {
			var compiled = CompileAsyncLambda<string, string>(paraInput =>
					paraInput);
			var result = await compiled("success").ConfigureAwait(false);
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
		public async Task TestBinarySyncSyncCompiled() {
			var compiled = CompileAsyncLambda<int, int>(paraInput =>
					Expression.Add(
							Expression.Constant(1),
							paraInput));
			var result = await compiled(2).ConfigureAwait(false);
			Assert.Equal(3, result);
		}

		[Fact]
		public async Task TestBinaryAsyncSyncCompiled() {
			var compiled = CompileAsyncLambda<int, int>(paraInput =>
					Expression.Add(
							Expression.Constant(Task.FromResult(1)).Await(false),
							paraInput));
			var result = await compiled(2).ConfigureAwait(false);
			Assert.Equal(3, result);
		}

		[Fact]
		public async Task TestBinarySyncAsyncCompiled() {
			var compiled = CompileAsyncLambda<Task<int>, int>(paraInput =>
					Expression.Add(
							Expression.Constant(1),
							paraInput.Await(false)));
			var result = await compiled(Task.FromResult(2)).ConfigureAwait(false);
			Assert.Equal(3, result);
		}

		[Fact]
		public async Task TestBinaryAsyncAsyncCompiled() {
			var compiled = CompileAsyncLambda<Task<int>, int>(paraInput =>
					Expression.Add(
							Expression.Constant(Task.FromResult(1)).Await(false),
							paraInput.Await(false)));
			var result = await compiled(Task.FromResult(2)).ConfigureAwait(false);
			Assert.Equal(3, result);
		}

		[Fact]
		public async Task TestGotoAsyncCompiled() {
			var varResult = Expression.Variable(typeof(int), "result");
			var lblTarget = Expression.Label(typeof(int), "target");
			var compiled = CompileAsyncLambda<Task<bool>, int>(paraInput =>
					Expression.Block(
							Expression.IfThen(
									paraInput.Await(false),
									Expression.Goto(lblTarget, Expression.Constant(Task.FromResult(1)).Await(false))),
							Throw<Task<int>>().Await(false),
							Expression.Add(
									Expression.Label(lblTarget, Expression.Constant(Task.FromResult(-1)).Await(false)),
									Expression.Constant(2))));
			var result = await compiled(Task.FromResult(true)).ConfigureAwait(false);
			Assert.Equal(3, result);
		}

		[Fact]
		public async Task TestGotoSyncCompiled() {
			var lblTarget = Expression.Label(typeof(int), "target");
			var compiled = CompileAsyncLambda<bool, int>(paraInput =>
					Expression.Block(
							Expression.IfThen(
									paraInput,
									Expression.Goto(lblTarget, Expression.Constant(1))),
							Throw<int>(),
							Expression.Add(
									Expression.Label(lblTarget, Expression.Constant(-1)),
									Expression.Constant(2))));
			var result = await compiled(true).ConfigureAwait(false);
			Assert.Equal(3, result);
		}

		[Fact]
		public async Task TestTryCatchCompiled() {
			var varEx = Expression.Variable(typeof(Exception), "ex");
			var compiled = CompileAsyncLambda<Task<bool>, int>(paraInput =>
					Expression.TryCatch(
							Throw<Task<int>>().Await(false),
							Expression.Catch(varEx,
									Expression.Constant(-1),
									Expression.Constant(true))));
			var result = await compiled(Task.FromResult(true)).ConfigureAwait(false);
			Assert.Equal(-1, result);
		}

		[Fact]
		public async Task TestTryFinallyCompiled() {
			var varResult = Expression.Variable(typeof(int), "result");
			var compiled = CompileAsyncLambda<Task<int>, int>(paraInput =>
					Expression.Block(varResult.Yield(),
							Expression.TryFinally(
									Expression.Assign(
											varResult,
											paraInput.Await(false)),
									Expression.AddAssign(
											varResult,
											Expression.Constant(Task.FromResult(1)).Await(false))),
							varResult));
			var result = await compiled(Task.FromResult(2)).ConfigureAwait(false);
			Assert.Equal(3, result);
		}

		[Fact]
		public async Task TestTryCatchFinallyCompiled() {
			var varEx = Expression.Variable(typeof(Exception), "ex");
			var varResult = Expression.Variable(typeof(int), "result");
			var compiled = CompileAsyncLambda<Task<int>, int>(paraInput =>
					Expression.Block(varResult.Yield(),
							Expression.TryCatchFinally(
									Throw<Task<int>>().Await(false),
									Expression.AddAssign(
											varResult,
											Expression.Constant(Task.FromResult(1)).Await(false)),
									Expression.Catch(varEx,
											Expression.Assign(
													varResult,
													paraInput.Await(false)),
											Expression.Constant(true))),
							varResult));
			var result = await compiled(Task.FromResult(2)).ConfigureAwait(false);
			Assert.Equal(3, result);
		}

		[Fact]
		public async Task TestTryFaultCompiled() {
			var compiled = CompileAsyncLambda<Task<int>, int>(paraInput =>
					Expression.TryFault(
							Expression.Add(
									paraInput.Await(false),
									Expression.Constant(1)),
							Expression.Constant(Task.FromResult(-1)).Await(false)));
			var result = await compiled(Task.FromResult(2)).ConfigureAwait(false);
			Assert.Equal(3, result);
		}

		[Fact]
		public async Task TestLoopAsyncCompiled() {
			var lblContinue = Expression.Label("continue");
			var lblBreak = Expression.Label(typeof(int), "break");
			var varResult = Expression.Variable(typeof(int), "result");
			var compiled = CompileAsyncLambda<Task<int>, int>(paraInput =>
					Expression.Add(
							Expression.Block(varResult.Yield(),
									Expression.Loop(
											Expression.Block(
													Expression.IfThen(
															Expression.LessThanOrEqual(
																	paraInput.Await(false),
																	varResult),
															Expression.Break(lblBreak, varResult)),
													Expression.AddAssign(
															varResult,
															Expression.Constant(Task.FromResult(1)).Await(false)),
													Expression.IfThen(
															Expression.LessThanOrEqual(
																	Expression.Constant(0),
																	varResult),
															Expression.Continue(lblContinue)),
													Throw<int>()),
											lblBreak, lblContinue)),
							Expression.Constant(Task.FromResult(1)).Await(false)));
			var result = await compiled(Task.FromResult(2)).ConfigureAwait(false);
			Assert.Equal(3, result);
		}

		[Fact]
		public async Task TestLoopSyncCompiled() {
			var lblContinue = Expression.Label("continue");
			var lblBreak = Expression.Label(typeof(int), "break");
			var varResult = Expression.Variable(typeof(int), "result");
			var compiled = CompileAsyncLambda<int, int>(paraInput =>
					Expression.Add(
							Expression.Block(varResult.Yield(),
									Expression.Loop(
											Expression.Block(
													Expression.IfThen(
															Expression.LessThanOrEqual(
																	paraInput,
																	varResult),
															Expression.Break(lblBreak, varResult)),
													Expression.AddAssign(
															varResult,
															Expression.Constant(1)),
													Expression.IfThen(
															Expression.LessThanOrEqual(
																	Expression.Constant(0),
																	varResult),
															Expression.Continue(lblContinue)),
													Throw<int>()),
											lblBreak, lblContinue)),
							Expression.Constant(1)));
			var result = await compiled(2).ConfigureAwait(false);
			Assert.Equal(3, result);
		}

		[Fact]
		public async Task TestSwitchSyncSyncCompiled() {
			var compiled = CompileAsyncLambda<Task<int>, int>(paraInput =>
					Expression.Switch(paraInput.Await(false),
							Expression.Constant(-1),
							Expression.SwitchCase(
									Throw<int>(),
									Expression.Constant(1)),
							Expression.SwitchCase(
									Throw<int>(),
									Expression.Constant(2))));
			var result = await compiled(Task.FromResult(0)).ConfigureAwait(false);
			Assert.Equal(-1, result);
		}

		[Fact]
		public async Task TestSwitchSyncAsyncCompiled() {
			var compiled = CompileAsyncLambda<Task<int>, int>(paraInput =>
					Expression.Switch(paraInput.Await(false),
							Expression.Constant(Task.FromResult(-1)).Await(false),
							Expression.SwitchCase(
									Throw<Task<int>>().Await(false),
									Expression.Constant(1)),
							Expression.SwitchCase(
									Throw<Task<int>>().Await(false),
									Expression.Constant(2))));
			var result = await compiled(Task.FromResult(0)).ConfigureAwait(false);
			Assert.Equal(-1, result);
		}

		[Fact]
		public async Task TestSwitchAsyncSyncCompiled() {
			var compiled = CompileAsyncLambda<Task<int>, int>(paraInput =>
					Expression.Switch(paraInput.Await(false),
							Expression.Constant(-1),
							Expression.SwitchCase(
									Throw<int>(),
									Expression.Constant(Task.FromResult(1)).Await(false)),
							Expression.SwitchCase(
									Throw<int>(),
									Expression.Constant(Task.FromResult(2)).Await(false))));
			var result = await compiled(Task.FromResult(0)).ConfigureAwait(false);
			Assert.Equal(-1, result);
		}

		[Fact]
		public async Task TestSwitchAsyncAsyncCompiled() {
			var compiled = CompileAsyncLambda<Task<int>, int>(paraInput =>
					Expression.Switch(paraInput.Await(false),
							Expression.Constant(Task.FromResult(-1)).Await(false),
							Expression.SwitchCase(
									Throw<Task<int>>().Await(false),
									Expression.Constant(Task.FromResult(1)).Await(false)),
							Expression.SwitchCase(
									Throw<Task<int>>().Await(false),
									Expression.Constant(Task.FromResult(2)).Await(false))));
			var result = await compiled(Task.FromResult(0)).ConfigureAwait(false);
			Assert.Equal(-1, result);
		}

		[Fact]
		public async Task TestSwitchMixSAAsyncCompiled() {
			var compiled = CompileAsyncLambda<Task<int>, int>(paraInput =>
					Expression.Switch(paraInput.Await(false),
							Expression.Constant(Task.FromResult(-1)).Await(false),
							Expression.SwitchCase(
									Throw<Task<int>>().Await(false),
									Expression.Constant(1)),
							Expression.SwitchCase(
									Throw<Task<int>>().Await(false),
									Expression.Constant(Task.FromResult(2)).Await(false))));
			var result = await compiled(Task.FromResult(0)).ConfigureAwait(false);
			Assert.Equal(-1, result);
		}

		[Fact]
		public async Task TestSwitchMixASAsyncCompiled() {
			var compiled = CompileAsyncLambda<Task<int>, int>(paraInput =>
					Expression.Switch(paraInput.Await(false),
							Expression.Constant(Task.FromResult(-1)).Await(false),
							Expression.SwitchCase(
									Throw<Task<int>>().Await(false),
									Expression.Constant(Task.FromResult(1)).Await(false),
									Expression.Constant(4)),
							Expression.SwitchCase(
									Throw<Task<int>>().Await(false),
									Expression.Constant(2),
									Expression.Constant(3))));
			var result = await compiled(Task.FromResult(0)).ConfigureAwait(false);
			Assert.Equal(-1, result);
		}

		[Fact]
		public async Task TestOrElseSyncSyncCompiled() {
			var compiled = CompileAsyncLambda<bool, bool>(paraInput =>
					Expression.OrElse(
							paraInput,
							Throw<bool>()));
			var result = await compiled(true).ConfigureAwait(false);
			Assert.True(result);
		}

		[Fact]
		public async Task TestOrElseAsyncSyncCompiled() {
			var compiled = CompileAsyncLambda<Task<bool>, bool>(paraInput =>
					Expression.OrElse(
							paraInput.Await(false),
							Throw<bool>()));
			var result = await compiled(Task.FromResult(true)).ConfigureAwait(false);
			Assert.True(result);
		}

		[Fact]
		public async Task TestOrElseSyncAsyncCompiled() {
			var compiled = CompileAsyncLambda<bool, bool>(paraInput =>
					Expression.OrElse(
							paraInput,
							Throw<Task<bool>>().Await(false)));
			var result = await compiled(true).ConfigureAwait(false);
			Assert.True(result);
		}

		[Fact]
		public async Task TestOrElseAsyncAsyncCompiled() {
			var compiled = CompileAsyncLambda<Task<bool>, bool>(paraInput =>
					Expression.OrElse(
							paraInput.Await(false),
							Throw<Task<bool>>().Await(false)));
			var result = await compiled(Task.FromResult(true)).ConfigureAwait(false);
			Assert.True(result);
		}

		[Fact]
		public async Task TestAndAlsoSyncSyncCompiled() {
			var compiled = CompileAsyncLambda<bool, bool>(paraInput =>
					Expression.AndAlso(
							paraInput,
							Throw<bool>()));
			var result = await compiled(false).ConfigureAwait(false);
			Assert.False(result);
		}

		[Fact]
		public async Task TestAndAlsoAsyncSyncCompiled() {
			var compiled = CompileAsyncLambda<Task<bool>, bool>(paraInput =>
					Expression.AndAlso(
							paraInput.Await(false),
							Throw<bool>()));
			var result = await compiled(Task.FromResult(false)).ConfigureAwait(false);
			Assert.False(result);
		}

		[Fact]
		public async Task TestAndAlsoSyncAsyncCompiled() {
			var compiled = CompileAsyncLambda<bool, bool>(paraInput =>
					Expression.AndAlso(
							paraInput,
							Throw<Task<bool>>().Await(false)));
			var result = await compiled(false).ConfigureAwait(false);
			Assert.False(result);
		}

		[Fact]
		public async Task TestAndAlsoAsyncAsyncCompiled() {
			var compiled = CompileAsyncLambda<Task<bool>, bool>(paraInput =>
					Expression.AndAlso(
							paraInput.Await(false),
							Throw<Task<bool>>().Await(false)));
			var result = await compiled(Task.FromResult(false)).ConfigureAwait(false);
			Assert.False(result);
		}

		[Fact]
		public async Task TestConditionalSyncSyncCompiled() {
			var compiled = CompileAsyncLambda<Task<bool>, string>(paraInput =>
					Expression.Condition(
							paraInput.Await(false),
							Expression.Constant("success"),
							Throw<string>()));
			var result = await compiled(Task.FromResult(true)).ConfigureAwait(false);
			Assert.Equal("success", result);
		}

		[Fact]
		public async Task TestConditionalAsyncSyncCompiled() {
			var compiled = CompileAsyncLambda<Task<bool>, string>(paraInput =>
					Expression.Condition(
							paraInput.Await(false),
							Expression.Constant(Task.FromResult("success")).Await(false),
							Throw<string>()));
			var result = await compiled(Task.FromResult(true)).ConfigureAwait(false);
			Assert.Equal("success", result);
		}

		[Fact]
		public async Task TestConditionalSyncAsyncCompiled() {
			var compiled = CompileAsyncLambda<Task<bool>, string>(paraInput =>
					Expression.Condition(
							paraInput.Await(false),
							Expression.Constant("success"),
							Throw<Task<string>>().Await(false)));
			var result = await compiled(Task.FromResult(true)).ConfigureAwait(false);
			Assert.Equal("success", result);
		}

		[Fact]
		public async Task TestConditionalAsyncAsyncCompiled() {
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
