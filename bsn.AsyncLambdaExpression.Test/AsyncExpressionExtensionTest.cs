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
	public class AsyncExpressionExtensionTest: ExpressionExtensionTestBase {
		public AsyncExpressionExtensionTest(ITestOutputHelper output): base(output) { }

		private Func<TInput, Task<TResult>> CompileAsyncLambda<TInput, TResult>(bool debug, Func<ParameterExpression, Expression> bodyFactory) {
			var paraInput = Expression.Parameter(typeof(TInput), "input");
			var exprLambda = Expression.Lambda<Func<TInput, TResult>>(bodyFactory(paraInput), paraInput);
			return this.CompileAsyncLambda<Func<TInput, Task<TResult>>>(debug, exprLambda);
		}

		private T CompileAsyncLambda<T>(bool debug, LambdaExpression exprLambda) {
			this.Output.WriteLine("==> Original Lambda");
			this.Output.WriteLine(exprLambda.ToString(BuiltinRenderer.DebugView));
			exprLambda.Compile();
			var exprAsync = exprLambda.Async<T>(debug);
			this.Output.WriteLine("");
			this.Output.WriteLine("==> Async Lambda");
			this.Output.WriteLine(exprAsync.ToString(BuiltinRenderer.DebugView));
			return exprAsync.Compile(false);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestReturnTaskSyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Func<Task>>(debug, Expression.Lambda<Action>(
					Expression.Empty()));
			await compiled().ConfigureAwait(false);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestReturnTaskTypedSyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Func<Task<string>>>(debug, Expression.Lambda<Action>(
					Expression.Constant("success")));
			var result = await compiled().ConfigureAwait(false);
			Assert.Equal("success", result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestReturnValueTaskSyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Func<ValueTask>>(debug, Expression.Lambda<Action>(
					Expression.Empty()));
			await compiled().ConfigureAwait(false);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestReturnValueTaskTypedSyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Func<ValueTask<string>>>(debug, Expression.Lambda<Action>(
					Expression.Constant("success")));
			var result = await compiled().ConfigureAwait(false);
			Assert.Equal("success", result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestReturnTaskAsyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Func<Task>>(debug, Expression.Lambda<Action>(
					Expression.Constant(Task.CompletedTask).AwaitConfigured(false)));
			await compiled().ConfigureAwait(false);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestReturnValueTaskAsyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Func<ValueTask>>(debug, Expression.Lambda<Action>(
					Expression.Constant(Task.CompletedTask).AwaitConfigured(false)));
			await compiled().ConfigureAwait(false);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestReturnValueTaskTypedAsyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Func<ValueTask<string>>>(debug, Expression.Lambda<Action>(
					Expression.Constant(Task.FromResult("success")).AwaitConfigured(false)));
			var result = await compiled().ConfigureAwait(false);
			Assert.Equal("success", result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestCompletedCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<string>, string>(debug, paraInput =>
					paraInput.AwaitConfigured(false));
			var result = await compiled(Task.FromResult("success")).ConfigureAwait(false);
			Assert.Equal("success", result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestSyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<string, string>(debug, paraInput =>
					paraInput);
			var result = await compiled("success").ConfigureAwait(false);
			Assert.Equal("success", result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestDelayCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<string>, string>(debug, paraInput =>
					Expression.Block(
							Expression.Call(typeof(Task), nameof(Task.Delay), null, Expression.Constant(50)).AwaitConfigured(false),
							paraInput.AwaitConfigured(false)));
			var result = await compiled(Task.FromResult("success")).ConfigureAwait(false);
			Assert.Equal("success", result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestBlockCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<string>, string>(debug, paraInput =>
					Expression.Block(
							Expression.Constant(Task.FromResult("not-used")).AwaitConfigured(false),
							paraInput.AwaitConfigured(false)));
			var result = await compiled(Task.FromResult("success")).ConfigureAwait(false);
			Assert.Equal("success", result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestBinarySyncSyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<int, int>(debug, paraInput =>
					Expression.Add(
							Expression.Constant(1),
							paraInput));
			var result = await compiled(2).ConfigureAwait(false);
			Assert.Equal(3, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestBinaryAsyncSyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<int, int>(debug, paraInput =>
					Expression.Add(
							Expression.Constant(Task.FromResult(1)).AwaitConfigured(false),
							paraInput));
			var result = await compiled(2).ConfigureAwait(false);
			Assert.Equal(3, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestBinarySyncAsyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<int>, int>(debug, paraInput =>
					Expression.Add(
							Expression.Constant(1),
							paraInput.AwaitConfigured(false)));
			var result = await compiled(Task.FromResult(2)).ConfigureAwait(false);
			Assert.Equal(3, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestBinaryAsyncAsyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<int>, int>(debug, paraInput =>
					Expression.Add(
							Expression.Constant(Task.FromResult(1)).AwaitConfigured(false),
							paraInput.AwaitConfigured(false)));
			var result = await compiled(Task.FromResult(2)).ConfigureAwait(false);
			Assert.Equal(3, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestGotoAsyncCompiled(bool debug) {
			var lblTarget = Expression.Label(typeof(void), "target");
			var varTemp = Expression.Variable(typeof(int), "temp");
			var compiled = this.CompileAsyncLambda<Task<bool>, int>(debug, paraInput =>
					Expression.Block(varTemp.Yield(),
							Expression.IfThen(
									paraInput.AwaitConfigured(false),
									Expression.Block(
											Expression.Assign(
													varTemp,
													Expression.Constant(1)),
											Expression.Goto(lblTarget))),
							Expression.Assign(
									varTemp,
									Expression.Constant(Task.FromResult(-1)).AwaitConfigured(false)),
							Expression.Label(lblTarget),
							Expression.Add(
									varTemp,
									Expression.Constant(Task.FromResult(2)).AwaitConfigured(false))));
			var result = await compiled(Task.FromResult(true)).ConfigureAwait(false);
			Assert.Equal(3, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestGotoSyncCompiled(bool debug) {
			var lblTarget = Expression.Label(typeof(void), "target");
			var varTemp = Expression.Variable(typeof(int), "temp");
			var compiled = this.CompileAsyncLambda<bool, int>(debug, paraInput =>
					Expression.Block(varTemp.Yield(),
							Expression.IfThen(
									paraInput,
									Expression.Block(
											Expression.Assign(
													varTemp,
													Expression.Constant(1)),
											Expression.Goto(lblTarget))),
							Expression.Assign(
									varTemp,
									Expression.Constant(-1)),
							Expression.Label(lblTarget),
							Expression.Add(
									varTemp,
									Expression.Constant(2))));
			var result = await compiled(true).ConfigureAwait(false);
			Assert.Equal(3, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestTryCatchCompiled(bool debug) {
			var varEx = Expression.Variable(typeof(Exception), "ex");
			var compiled = this.CompileAsyncLambda<Task<bool>, int>(debug, paraInput =>
					Expression.TryCatch(
							Throw<Task<int>>().AwaitConfigured(false),
							Expression.Catch(varEx,
									Expression.Constant(-1))));
			var result = await compiled(Task.FromResult(true)).ConfigureAwait(false);
			Assert.Equal(-1, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestTryFinallyAsyncCompiled(bool debug) {
			var varResult = Expression.Variable(typeof(int), "result");
			var compiled = this.CompileAsyncLambda<Task<int>, int>(debug, paraInput =>
					Expression.Block(varResult.Yield(),
							Expression.TryFinally(
									Expression.Assign(
											varResult,
											paraInput.AwaitConfigured(false)),
									Expression.AddAssign(
											varResult,
											Expression.Constant(Task.FromResult(1)).AwaitConfigured(false))),
							varResult));
			var result = await compiled(Task.FromResult(2)).ConfigureAwait(false);
			Assert.Equal(3, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestTryFinallySyncCompiled(bool debug) {
			var varResult = Expression.Variable(typeof(int), "result");
			var compiled = this.CompileAsyncLambda<int, int>(debug, paraInput =>
					Expression.Block(varResult.Yield(),
							Expression.TryFinally(
									Expression.Assign(
											varResult,
											paraInput),
									Expression.AddAssign(
											varResult,
											Expression.Constant(1))),
							varResult));
			var result = await compiled(2).ConfigureAwait(false);
			Assert.Equal(3, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestTryFinallyThrowCompiled(bool debug) {
			var varResult = Expression.Variable(typeof(int), "result");
			var compiled = this.CompileAsyncLambda<int, int>(debug, paraInput =>
					Expression.Block(varResult.Yield(),
							Expression.TryFinally(
									Expression.Assign(
											varResult,
											paraInput),
									Throw<object>("Stop")),
							varResult));
			var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await compiled(2).ConfigureAwait(false)).ConfigureAwait(false);
			Assert.Equal("Stop", ex.Message);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestTryThrowFinallyCompiled(bool debug) {
			var varResult = Expression.Variable(typeof(int), "result");
			var compiled = this.CompileAsyncLambda<int, int>(debug, paraInput =>
					Expression.Block(varResult.Yield(),
							Expression.TryFinally(
									Throw<object>("Stop"),
									Expression.Assign(
											varResult,
											paraInput)),
							varResult));
			var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await compiled(2).ConfigureAwait(false)).ConfigureAwait(false);
			Assert.Equal("Stop", ex.Message);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestTryCatchFinallyAsyncCompiled(bool debug) {
			var varEx = Expression.Variable(typeof(Exception), "ex");
			var varResult = Expression.Variable(typeof(int), "result");
			var compiled = this.CompileAsyncLambda<Task<int>, int>(debug, paraInput =>
					Expression.Block(varResult.Yield(),
							Expression.TryCatchFinally(
									Throw<Task<int>>("Exception should be swallowed").AwaitConfigured(false),
									Expression.AddAssign(
											varResult,
											Expression.Constant(Task.FromResult(1)).AwaitConfigured(false)),
									Expression.Catch(varEx,
											Expression.Assign(
													varResult,
													paraInput.AwaitConfigured(false)))),
							varResult));
			var result = await compiled(Task.FromResult(2)).ConfigureAwait(false);
			Assert.Equal(3, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestTryCatchFinallySyncCompiled(bool debug) {
			var varEx = Expression.Variable(typeof(Exception), "ex");
			var varResult = Expression.Variable(typeof(int), "result");
			var compiled = this.CompileAsyncLambda<int, int>(debug, paraInput =>
					Expression.Block(varResult.Yield(),
							Expression.TryCatchFinally(
									Throw<int>("Exception should be swallowed"),
									Expression.AddAssign(
											varResult,
											Expression.Constant(1)),
									Expression.Catch(varEx,
											Expression.Assign(
													varResult,
													paraInput))),
							varResult));
			var result = await compiled(2).ConfigureAwait(false);
			Assert.Equal(3, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestTryCatchFinallyNestedSync1Compiled(bool debug) {
			var varEx = Expression.Variable(typeof(InvalidOperationException), "ex");
			var varResult = Expression.Variable(typeof(int), "result");
			var compiled = this.CompileAsyncLambda<int, int>(debug, paraInput =>
					Expression.Block(varResult.Yield(),
							Expression.TryCatchFinally(
									Expression.TryCatchFinally(
											Throw<Task<int>>("Exception should be swallowed").AwaitConfigured(false),
											Expression.AddAssign(
													varResult,
													Expression.Constant(1)),
											Expression.Catch(varEx,
													Expression.Assign(
															varResult,
															paraInput))),
									Expression.AddAssign(
											varResult,
											Expression.Constant(10)),
									Expression.Catch(typeof(Exception),
											Expression.AddAssign(
													varResult,
													Expression.Constant(100)))),
							varResult));
			var result = await compiled(2).ConfigureAwait(false);
			Assert.Equal(13, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestTryCatchFinallyNestedSync2Compiled(bool debug) {
			var varEx = Expression.Variable(typeof(ArgumentException), "ex");
			var varResult = Expression.Variable(typeof(int), "result");
			var compiled = this.CompileAsyncLambda<int, int>(debug, paraInput =>
					Expression.Block(varResult.Yield(),
							Expression.TryCatchFinally(
									Expression.TryCatchFinally(
											Throw<Task<int>>("Exception should be swallowed").AwaitConfigured(false),
											Expression.AddAssign(
													varResult,
													Expression.Constant(1)),
											Expression.Catch(varEx,
													Expression.Assign(
															varResult,
															paraInput))),
									Expression.AddAssign(
											varResult,
											Expression.Constant(10)),
									Expression.Catch(typeof(Exception),
											Expression.AddAssign(
													varResult,
													Expression.Constant(100)))),
							varResult));
			var result = await compiled(2).ConfigureAwait(false);
			Assert.Equal(111, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestTryCatchFinallyNestedSync3Compiled(bool debug) {
			var varEx = Expression.Variable(typeof(ArgumentException), "ex");
			var varResult = Expression.Variable(typeof(int), "result");
			var compiled = this.CompileAsyncLambda<int, int>(debug, paraInput =>
					Expression.Block(varResult.Yield(),
							Expression.TryCatchFinally(
									Expression.TryCatchFinally(
											Expression.Constant(Task.FromResult(0)).Await(),
											Expression.AddAssign(
													varResult,
													Expression.Constant(1)),
											Expression.Catch(varEx,
													Expression.Assign(
															varResult,
															paraInput))),
									Expression.AddAssign(
											varResult,
											Expression.Constant(10)),
									Expression.Catch(typeof(Exception),
											Expression.AddAssign(
													varResult,
													Expression.Constant(100)))),
							varResult));
			var result = await compiled(2).ConfigureAwait(false);
			Assert.Equal(11, result);
		}

		[Fact(Skip = "System.Reflection.Emit.DynamicILGenerator.BeginFaultBlock() is not supported on DynamicMethod of .NET 4.8")]
		public async Task TestTryFaultSyncCompiled() {
			var compiled = this.CompileAsyncLambda<int, int>(true, paraInput =>
					Expression.TryFault(
							Expression.Add(
									paraInput,
									Expression.Constant(1)),
							Expression.Constant(-1)));
			var result = await compiled(2).ConfigureAwait(false);
			Assert.Equal(3, result);
		}

		[Fact(Skip = "System.Reflection.Emit.DynamicILGenerator.BeginFaultBlock() is not supported on DynamicMethod of .NET 4.8")]
		public async Task TestTryFaultAsyncCompiled() {
			var compiled = this.CompileAsyncLambda<Task<int>, int>(true, paraInput =>
					Expression.TryFault(
							Expression.Add(
									paraInput.AwaitConfigured(false),
									Expression.Constant(1)),
							Expression.Constant(Task.FromResult(-1)).AwaitConfigured(false)));
			var result = await compiled(Task.FromResult(2)).ConfigureAwait(false);
			Assert.Equal(3, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestLoopAsyncCompiled(bool debug) {
			var lblContinue = Expression.Label("continue");
			var lblBreak = Expression.Label(typeof(int), "break");
			var varResult = Expression.Variable(typeof(int), "result");
			var compiled = this.CompileAsyncLambda<Task<int>, int>(debug, paraInput =>
					Expression.Add(
							Expression.Block(varResult.Yield(),
									Expression.Loop(
											Expression.Block(
													Expression.IfThen(
															Expression.LessThanOrEqual(
																	paraInput.AwaitConfigured(false),
																	varResult),
															Expression.Break(lblBreak, varResult)),
													Expression.AddAssign(
															varResult,
															Expression.Constant(Task.FromResult(1)).AwaitConfigured(false)),
													Expression.IfThen(
															Expression.LessThanOrEqual(
																	Expression.Constant(0),
																	varResult),
															Expression.Continue(lblContinue)),
													Throw<int>()),
											lblBreak, lblContinue)),
							Expression.Constant(Task.FromResult(1)).AwaitConfigured(false)));
			var result = await compiled(Task.FromResult(2)).ConfigureAwait(false);
			Assert.Equal(3, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestLoopSyncCompiled(bool debug) {
			var lblContinue = Expression.Label("continue");
			var lblBreak = Expression.Label(typeof(int), "break");
			var varResult = Expression.Variable(typeof(int), "result");
			var compiled = this.CompileAsyncLambda<int, int>(debug, paraInput =>
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

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestSwitchSyncSyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<int>, int>(debug, paraInput =>
					Expression.Switch(paraInput.AwaitConfigured(false),
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

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestSwitchSyncAsyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<int>, int>(debug, paraInput =>
					Expression.Switch(paraInput.AwaitConfigured(false),
							Expression.Constant(Task.FromResult(-1)).AwaitConfigured(false),
							Expression.SwitchCase(
									Throw<Task<int>>().AwaitConfigured(false),
									Expression.Constant(1)),
							Expression.SwitchCase(
									Throw<Task<int>>().AwaitConfigured(false),
									Expression.Constant(2))));
			var result = await compiled(Task.FromResult(0)).ConfigureAwait(false);
			Assert.Equal(-1, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestSwitchAsyncSyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<int>, int>(debug, paraInput =>
					Expression.Switch(paraInput.AwaitConfigured(false),
							Expression.Constant(-1),
							Expression.SwitchCase(
									Throw<int>(),
									Expression.Constant(Task.FromResult(1)).AwaitConfigured(false)),
							Expression.SwitchCase(
									Throw<int>(),
									Expression.Constant(Task.FromResult(2)).AwaitConfigured(false))));
			var result = await compiled(Task.FromResult(0)).ConfigureAwait(false);
			Assert.Equal(-1, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestSwitchAsyncAsyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<int>, int>(debug, paraInput =>
					Expression.Switch(paraInput.AwaitConfigured(false),
							Expression.Constant(Task.FromResult(-1)).AwaitConfigured(false),
							Expression.SwitchCase(
									Throw<Task<int>>().AwaitConfigured(false),
									Expression.Constant(Task.FromResult(1)).AwaitConfigured(false)),
							Expression.SwitchCase(
									Throw<Task<int>>().AwaitConfigured(false),
									Expression.Constant(Task.FromResult(2)).AwaitConfigured(false))));
			var result = await compiled(Task.FromResult(0)).ConfigureAwait(false);
			Assert.Equal(-1, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestSwitchMixSAAsyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<int>, int>(debug, paraInput =>
					Expression.Switch(paraInput.AwaitConfigured(false),
							Expression.Constant(Task.FromResult(-1)).AwaitConfigured(false),
							Expression.SwitchCase(
									Throw<Task<int>>().AwaitConfigured(false),
									Expression.Constant(1)),
							Expression.SwitchCase(
									Throw<Task<int>>().AwaitConfigured(false),
									Expression.Constant(Task.FromResult(2)).AwaitConfigured(false))));
			var result = await compiled(Task.FromResult(0)).ConfigureAwait(false);
			Assert.Equal(-1, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestSwitchMixASAsyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<int>, int>(debug, paraInput =>
					Expression.Switch(paraInput.AwaitConfigured(false),
							Expression.Constant(Task.FromResult(-1)).AwaitConfigured(false),
							Expression.SwitchCase(
									Throw<Task<int>>().AwaitConfigured(false),
									Expression.Constant(Task.FromResult(1)).AwaitConfigured(false),
									Expression.Constant(4)),
							Expression.SwitchCase(
									Throw<Task<int>>().AwaitConfigured(false),
									Expression.Constant(2),
									Expression.Constant(3))));
			var result = await compiled(Task.FromResult(0)).ConfigureAwait(false);
			Assert.Equal(-1, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestOrElseSyncSyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<bool, bool>(debug, paraInput =>
					Expression.OrElse(
							paraInput,
							Throw<bool>()));
			var result = await compiled(true).ConfigureAwait(false);
			Assert.True(result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestOrElseAsyncSyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<bool>, bool>(debug, paraInput =>
					Expression.OrElse(
							paraInput.AwaitConfigured(false),
							Throw<bool>()));
			var result = await compiled(Task.FromResult(true)).ConfigureAwait(false);
			Assert.True(result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestOrElseSyncAsyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<bool, bool>(debug, paraInput =>
					Expression.OrElse(
							paraInput,
							Throw<Task<bool>>().AwaitConfigured(false)));
			var result = await compiled(true).ConfigureAwait(false);
			Assert.True(result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestOrElseAsyncAsyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<bool>, bool>(debug, paraInput =>
					Expression.OrElse(
							paraInput.AwaitConfigured(false),
							Throw<Task<bool>>().AwaitConfigured(false)));
			var result = await compiled(Task.FromResult(true)).ConfigureAwait(false);
			Assert.True(result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestAndAlsoSyncSyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<bool, bool>(debug, paraInput =>
					Expression.AndAlso(
							paraInput,
							Throw<bool>()));
			var result = await compiled(false).ConfigureAwait(false);
			Assert.False(result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestAndAlsoAsyncSyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<bool>, bool>(debug, paraInput =>
					Expression.AndAlso(
							paraInput.AwaitConfigured(false),
							Throw<bool>()));
			var result = await compiled(Task.FromResult(false)).ConfigureAwait(false);
			Assert.False(result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestAndAlsoSyncAsyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<bool, bool>(debug, paraInput =>
					Expression.AndAlso(
							paraInput,
							Throw<Task<bool>>().AwaitConfigured(false)));
			var result = await compiled(false).ConfigureAwait(false);
			Assert.False(result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestAndAlsoAsyncAsyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<bool>, bool>(debug, paraInput =>
					Expression.AndAlso(
							paraInput.AwaitConfigured(false),
							Throw<Task<bool>>().AwaitConfigured(false)));
			var result = await compiled(Task.FromResult(false)).ConfigureAwait(false);
			Assert.False(result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestConditionalSyncSyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<bool>, string>(debug, paraInput =>
					Expression.Condition(
							paraInput.AwaitConfigured(false),
							Expression.Constant("success"),
							Throw<string>()));
			var result = await compiled(Task.FromResult(true)).ConfigureAwait(false);
			Assert.Equal("success", result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestConditionalAsyncSyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<bool>, string>(debug, paraInput =>
					Expression.Condition(
							paraInput.AwaitConfigured(false),
							Expression.Constant(Task.FromResult("success")).AwaitConfigured(false),
							Throw<string>()));
			var result = await compiled(Task.FromResult(true)).ConfigureAwait(false);
			Assert.Equal("success", result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestConditionalSyncAsyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<bool>, string>(debug, paraInput =>
					Expression.Condition(
							paraInput.AwaitConfigured(false),
							Expression.Constant("success"),
							Throw<Task<string>>().AwaitConfigured(false)));
			var result = await compiled(Task.FromResult(true)).ConfigureAwait(false);
			Assert.Equal("success", result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestConditionalAsyncAsyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<bool>, string>(debug, paraInput =>
					Expression.Condition(
							paraInput.AwaitConfigured(false),
							Expression.Constant(Task.FromResult("success")).AwaitConfigured(false),
							Throw<Task<string>>().AwaitConfigured(false)));
			var result = await compiled(Task.FromResult(true)).ConfigureAwait(false);
			Assert.Equal("success", result);
		}

		[Fact]
		public void TestInvalidAwait() {
			Assert.Throws<ArgumentException>(() => Expression.Constant("test").Await());
		}

		[Fact]
		public void TestInvalidConfigureAwait() {
			Assert.Throws<ArgumentException>(() => Expression.Constant(Task.Yield()).AwaitConfigured(false));
		}

		[Fact]
		public void TestAwaitType() {
			var exprAwait = Expression.Constant(Task.FromResult("test")).Await();
			this.Output.WriteLine(exprAwait.ToString(BuiltinRenderer.DebugView));
			Assert.Equal(typeof(string), exprAwait.Type);
		}

		[Fact]
		public void TestAwaitConfiguredType() {
			var exprAwait = Expression.Constant(Task.FromResult("test")).AwaitConfigured(false);
			this.Output.WriteLine(exprAwait.ToString(BuiltinRenderer.DebugView));
			Assert.Equal(typeof(string), exprAwait.Type);
		}
	}
}
