using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using bsn.AsyncLambdaExpression.Expressions;

using ExpressionTreeToString;

using Xunit;
using Xunit.Abstractions;

namespace bsn.AsyncLambdaExpression {
	public class AsyncExpressionExtensionTest: ExpressionExtensionTestBase {
		public AsyncExpressionExtensionTest(ITestOutputHelper output): base(output) { }

		private Func<TInput, TResult> CompileAsyncLambda<TInput, TResult>(bool debug, Func<ParameterExpression, Expression> bodyFactory) {
			var paraInput = Expression.Parameter(typeof(TInput), "input");
			var exprLambda = AsyncExpression.AsyncLambda<Func<TInput, TResult>>(bodyFactory(paraInput), paraInput);
			return this.CompileStateMachineLambda(debug, exprLambda);
		}

		private TDelegate CompileStateMachineLambda<TDelegate>(bool debug, StateMachineLambdaExpression<TDelegate> exprLambda) where TDelegate: Delegate {
			this.Output.WriteLine("==> Original Lambda");
			this.Output.WriteLine(exprLambda.ToString(BuiltinRenderer.DebugView));
			exprLambda.Compile();
			var exprAsync = exprLambda.BuildLambdaExpression(debug ? DebugInfoGenerator.CreatePdbGenerator() : null);
			this.Output.WriteLine("");
			this.Output.WriteLine("==> Async Lambda");
			this.Output.WriteLine(exprAsync.ToString(BuiltinRenderer.DebugView));
			return exprAsync.Compile(false);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestReturnTaskSyncCompiled(bool debug) {
			var compiled = this.CompileStateMachineLambda(debug, AsyncExpression.AsyncLambda<Func<Task>>(
					Expression.Empty()));
			await compiled();
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestReturnTaskTypedSyncCompiled(bool debug) {
			var compiled = this.CompileStateMachineLambda(debug, AsyncExpression.AsyncLambda<Func<Task<string>>>(
					Expression.Constant("success")));
			var result = await compiled();
			Assert.Equal("success", result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestReturnValueTaskSyncCompiled(bool debug) {
			var compiled = this.CompileStateMachineLambda(debug, AsyncExpression.AsyncLambda<Func<ValueTask>>(
					Expression.Empty()));
			await compiled();
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestReturnValueTaskTypedSyncCompiled(bool debug) {
			var compiled = this.CompileStateMachineLambda(debug, AsyncExpression.AsyncLambda<Func<ValueTask<string>>>(
					Expression.Constant("success")));
			var result = await compiled();
			Assert.Equal("success", result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestReturnTaskAsyncCompiled(bool debug) {
			var compiled = this.CompileStateMachineLambda(debug,
					AsyncExpression.AsyncLambda<Func<Task>>(
							AsyncExpression.AwaitConfigured(
									Expression.Constant(Task.CompletedTask), false)));
			await compiled();
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestReturnValueTaskAsyncCompiled(bool debug) {
			var compiled = this.CompileStateMachineLambda(debug,
					AsyncExpression.AsyncLambda<Func<ValueTask>>(
							AsyncExpression.AwaitConfigured(
									Expression.Constant(Task.CompletedTask), false)));
			await compiled();
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestReturnValueTaskTypedAsyncCompiled(bool debug) {
			var compiled = this.CompileStateMachineLambda(debug,
					AsyncExpression.AsyncLambda<Func<ValueTask<string>>>(
							AsyncExpression.AwaitConfigured(
									Expression.Constant(Task.FromResult("success")), false)));
			var result = await compiled();
			Assert.Equal("success", result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestCompletedCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<string>, Task<string>>(debug, paraInput =>
					AsyncExpression.AwaitConfigured(paraInput, false));
			var result = await compiled(Task.FromResult("success"));
			Assert.Equal("success", result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestSyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<string, Task<string>>(debug, paraInput =>
					paraInput);
			var result = await compiled("success");
			Assert.Equal("success", result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestDelayCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<string>, Task<string>>(debug, paraInput =>
					Expression.Block(
							AsyncExpression.AwaitConfigured(
									Expression.Call(typeof(Task), nameof(Task.Delay), null, Expression.Constant(50)), false),
							AsyncExpression.AwaitConfigured(
									paraInput, false)));
			var result = await compiled(Task.FromResult("success"));
			Assert.Equal("success", result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestBlockCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<string>, Task<string>>(debug, paraInput =>
					Expression.Block(
							AsyncExpression.AwaitConfigured(
									Expression.Constant(Task.FromResult("not-used")), false),
							AsyncExpression.AwaitConfigured(
									paraInput, false)));
			var result = await compiled(Task.FromResult("success"));
			Assert.Equal("success", result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestBinarySyncSyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<int, Task<int>>(debug, paraInput =>
					Expression.Add(
							Expression.Constant(1),
							paraInput));
			var result = await compiled(2);
			Assert.Equal(3, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestBinaryAsyncSyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<int, Task<int>>(debug, paraInput =>
					Expression.Add(
							AsyncExpression.AwaitConfigured(
									Expression.Constant(Task.FromResult(1)), false),
							paraInput));
			var result = await compiled(2);
			Assert.Equal(3, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestBinarySyncAsyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<int>, Task<int>>(debug, paraInput =>
					Expression.Add(
							Expression.Constant(1),
							AsyncExpression.AwaitConfigured(
									paraInput, false)));
			var result = await compiled(Task.FromResult(2));
			Assert.Equal(3, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestBinaryAsyncAsyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<int>, Task<int>>(debug, paraInput =>
					Expression.Add(
							AsyncExpression.AwaitConfigured(
									Expression.Constant(Task.FromResult(1)), false),
							AsyncExpression.AwaitConfigured(
									paraInput, false)));
			var result = await compiled(Task.FromResult(2));
			Assert.Equal(3, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestGotoAsyncCompiled(bool debug) {
			var lblTarget = Expression.Label(typeof(void), "target");
			var varTemp = Expression.Variable(typeof(int), "temp");
			var compiled = this.CompileAsyncLambda<Task<bool>, Task<int>>(debug, paraInput =>
					Expression.Block(varTemp.Yield(),
							Expression.IfThen(
									AsyncExpression.AwaitConfigured(
											paraInput, false),
									Expression.Block(
											Expression.Assign(
													varTemp,
													Expression.Constant(1)),
											Expression.Goto(lblTarget))),
							Expression.Assign(
									varTemp,
									AsyncExpression.AwaitConfigured(
											Expression.Constant(Task.FromResult(-1)), false)),
							Expression.Label(lblTarget),
							Expression.Add(
									varTemp,
									AsyncExpression.AwaitConfigured(
											Expression.Constant(Task.FromResult(2)), false))));
			var result = await compiled(Task.FromResult(true));
			Assert.Equal(3, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestGotoSyncCompiled(bool debug) {
			var lblTarget = Expression.Label(typeof(void), "target");
			var varTemp = Expression.Variable(typeof(int), "temp");
			var compiled = this.CompileAsyncLambda<bool, Task<int>>(debug, paraInput =>
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
			var result = await compiled(true);
			Assert.Equal(3, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestTryCatchCompiled(bool debug) {
			var varEx = Expression.Variable(typeof(Exception), "ex");
			var compiled = this.CompileAsyncLambda<Task<bool>, Task<int>>(debug, paraInput =>
					Expression.TryCatch(
							AsyncExpression.AwaitConfigured(
									Throw<Task<int>>(), false),
							Expression.Catch(varEx,
									Expression.Constant(-1))));
			var result = await compiled(Task.FromResult(true));
			Assert.Equal(-1, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestTryFinallyAsyncCompiled(bool debug) {
			var varResult = Expression.Variable(typeof(int), "result");
			var compiled = this.CompileAsyncLambda<Task<int>, Task<int>>(debug, paraInput =>
					Expression.Block(varResult.Yield(),
							Expression.TryFinally(
									Expression.Assign(
											varResult,
											AsyncExpression.AwaitConfigured(
													paraInput, false)),
									Expression.AddAssign(
											varResult,
											AsyncExpression.AwaitConfigured(
													Expression.Constant(Task.FromResult(1)), false))),
							varResult));
			var result = await compiled(Task.FromResult(2));
			Assert.Equal(3, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestTryFinallySyncCompiled(bool debug) {
			var varResult = Expression.Variable(typeof(int), "result");
			var compiled = this.CompileAsyncLambda<int, Task<int>>(debug, paraInput =>
					Expression.Block(varResult.Yield(),
							Expression.TryFinally(
									Expression.Assign(
											varResult,
											paraInput),
									Expression.AddAssign(
											varResult,
											Expression.Constant(1))),
							varResult));
			var result = await compiled(2);
			Assert.Equal(3, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestTryFinallyThrowCompiled(bool debug) {
			var varResult = Expression.Variable(typeof(int), "result");
			var compiled = this.CompileAsyncLambda<int, Task<int>>(debug, paraInput =>
					Expression.Block(varResult.Yield(),
							Expression.TryFinally(
									Expression.Assign(
											varResult,
											paraInput),
									Throw<object>("Stop")),
							varResult));
			var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await compiled(2));
			Assert.Equal("Stop", ex.Message);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestTryThrowFinallyCompiled(bool debug) {
			var varResult = Expression.Variable(typeof(int), "result");
			var compiled = this.CompileAsyncLambda<int, Task<int>>(debug, paraInput =>
					Expression.Block(varResult.Yield(),
							Expression.TryFinally(
									Throw<object>("Stop"),
									Expression.Assign(
											varResult,
											paraInput)),
							varResult));
			var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await compiled(2));
			Assert.Equal("Stop", ex.Message);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestTryCatchFinallyAsyncCompiled(bool debug) {
			var varEx = Expression.Variable(typeof(Exception), "ex");
			var varResult = Expression.Variable(typeof(int), "result");
			var compiled = this.CompileAsyncLambda<Task<int>, Task<int>>(debug, paraInput =>
					Expression.Block(varResult.Yield(),
							Expression.TryCatchFinally(
									AsyncExpression.AwaitConfigured(
											Throw<Task<int>>("Exception should be swallowed"), false),
									Expression.AddAssign(
											varResult,
											AsyncExpression.AwaitConfigured(
													Expression.Constant(Task.FromResult(1)), false)),
									Expression.Catch(varEx,
											Expression.Assign(
													varResult,
													AsyncExpression.AwaitConfigured(
															paraInput, false)))),
							varResult));
			var result = await compiled(Task.FromResult(2));
			Assert.Equal(3, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestTryCatchFinallySyncCompiled(bool debug) {
			var varEx = Expression.Variable(typeof(Exception), "ex");
			var varResult = Expression.Variable(typeof(int), "result");
			var compiled = this.CompileAsyncLambda<int, Task<int>>(debug, paraInput =>
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
			var result = await compiled(2);
			Assert.Equal(3, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestTryCatchFinallyNestedSync1Compiled(bool debug) {
			var varEx = Expression.Variable(typeof(InvalidOperationException), "ex");
			var varResult = Expression.Variable(typeof(int), "result");
			var compiled = this.CompileAsyncLambda<int, Task<int>>(debug, paraInput =>
					Expression.Block(varResult.Yield(),
							Expression.TryCatchFinally(
									Expression.TryCatchFinally(
											AsyncExpression.AwaitConfigured(
													Throw<Task<int>>("Exception should be swallowed"), false),
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
			var result = await compiled(2);
			Assert.Equal(13, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestTryCatchFinallyNestedSync2Compiled(bool debug) {
			var varEx = Expression.Variable(typeof(ArgumentException), "ex");
			var varResult = Expression.Variable(typeof(int), "result");
			var compiled = this.CompileAsyncLambda<int, Task<int>>(debug, paraInput =>
					Expression.Block(varResult.Yield(),
							Expression.TryCatchFinally(
									Expression.TryCatchFinally(
											AsyncExpression.AwaitConfigured(
													Throw<Task<int>>("Exception should be swallowed"), false),
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
			var result = await compiled(2);
			Assert.Equal(111, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestTryCatchFinallyNestedSync3Compiled(bool debug) {
			var varEx = Expression.Variable(typeof(ArgumentException), "ex");
			var varResult = Expression.Variable(typeof(int), "result");
			var compiled = this.CompileAsyncLambda<int, Task<int>>(debug, paraInput =>
					Expression.Block(varResult.Yield(),
							Expression.TryCatchFinally(
									Expression.TryCatchFinally(
											AsyncExpression.Await(
													Expression.Constant(Task.FromResult(0))),
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
			var result = await compiled(2);
			Assert.Equal(11, result);
		}

		[Fact(Skip = "System.Reflection.Emit.DynamicILGenerator.BeginFaultBlock() is not supported on DynamicMethod of .NET 4.8")]
		public async Task TestTryFaultSyncCompiled() {
			var compiled = this.CompileAsyncLambda<int, Task<int>>(true, paraInput =>
					Expression.TryFault(
							Expression.Add(
									paraInput,
									Expression.Constant(1)),
							Expression.Constant(-1)));
			var result = await compiled(2);
			Assert.Equal(3, result);
		}

		[Fact(Skip = "System.Reflection.Emit.DynamicILGenerator.BeginFaultBlock() is not supported on DynamicMethod of .NET 4.8")]
		public async Task TestTryFaultAsyncCompiled() {
			var compiled = this.CompileAsyncLambda<Task<int>, Task<int>>(true, paraInput =>
					Expression.TryFault(
							Expression.Add(
									AsyncExpression.AwaitConfigured(
											paraInput, false),
									Expression.Constant(1)),
							AsyncExpression.AwaitConfigured(
									Expression.Constant(Task.FromResult(-1)), false)));
			var result = await compiled(Task.FromResult(2));
			Assert.Equal(3, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestLoopAsyncCompiled(bool debug) {
			var lblContinue = Expression.Label("continue");
			var lblBreak = Expression.Label(typeof(int), "break");
			var varResult = Expression.Variable(typeof(int), "result");
			var compiled = this.CompileAsyncLambda<Task<int>, Task<int>>(debug, paraInput =>
					Expression.Add(
							Expression.Block(varResult.Yield(),
									Expression.Loop(
											Expression.Block(
													Expression.IfThen(
															Expression.LessThanOrEqual(
																	AsyncExpression.AwaitConfigured(
																			paraInput, false),
																	varResult),
															Expression.Break(lblBreak, varResult)),
													Expression.AddAssign(
															varResult,
															AsyncExpression.AwaitConfigured(
																	Expression.Constant(Task.FromResult(1)), false)),
													Expression.IfThen(
															Expression.LessThanOrEqual(
																	Expression.Constant(0),
																	varResult),
															Expression.Continue(lblContinue)),
													Throw<int>()),
											lblBreak, lblContinue)),
							AsyncExpression.AwaitConfigured(
									Expression.Constant(Task.FromResult(1)), false)));
			var result = await compiled(Task.FromResult(2));
			Assert.Equal(3, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestLoopSyncCompiled(bool debug) {
			var lblContinue = Expression.Label("continue");
			var lblBreak = Expression.Label(typeof(int), "break");
			var varResult = Expression.Variable(typeof(int), "result");
			var compiled = this.CompileAsyncLambda<int, Task<int>>(debug, paraInput =>
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
			var result = await compiled(2);
			Assert.Equal(3, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestSwitchSyncSyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<int>, Task<int>>(debug, paraInput =>
					Expression.Switch(
							AsyncExpression.AwaitConfigured(
									paraInput, false),
							Expression.Constant(-1),
							Expression.SwitchCase(
									Throw<int>(),
									Expression.Constant(1)),
							Expression.SwitchCase(
									Throw<int>(),
									Expression.Constant(2))));
			var result = await compiled(Task.FromResult(0));
			Assert.Equal(-1, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestSwitchSyncAsyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<int>, Task<int>>(debug, paraInput =>
					Expression.Switch(
							AsyncExpression.AwaitConfigured(
									paraInput, false),
							AsyncExpression.AwaitConfigured(
									Expression.Constant(Task.FromResult(-1)), false),
							Expression.SwitchCase(
									AsyncExpression.AwaitConfigured(
											Throw<Task<int>>(), false),
									Expression.Constant(1)),
							Expression.SwitchCase(
									AsyncExpression.AwaitConfigured(
											Throw<Task<int>>(), false),
									Expression.Constant(2))));
			var result = await compiled(Task.FromResult(0));
			Assert.Equal(-1, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestSwitchAsyncSyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<int>, Task<int>>(debug, paraInput =>
					Expression.Switch(
							AsyncExpression.AwaitConfigured(
									paraInput, false),
							Expression.Constant(-1),
							Expression.SwitchCase(
									Throw<int>(),
									AsyncExpression.AwaitConfigured(
											Expression.Constant(Task.FromResult(1)), false)),
							Expression.SwitchCase(
									Throw<int>(),
									AsyncExpression.AwaitConfigured(
											Expression.Constant(Task.FromResult(2)), false))));
			var result = await compiled(Task.FromResult(0));
			Assert.Equal(-1, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestSwitchAsyncAsyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<int>, Task<int>>(debug, paraInput =>
					Expression.Switch(
							AsyncExpression.AwaitConfigured(
									paraInput, false),
							AsyncExpression.AwaitConfigured(
									Expression.Constant(Task.FromResult(-1)), false),
							Expression.SwitchCase(
									AsyncExpression.AwaitConfigured(
											Throw<Task<int>>(), false),
									AsyncExpression.AwaitConfigured(
											Expression.Constant(Task.FromResult(1)), false)),
							Expression.SwitchCase(
									AsyncExpression.AwaitConfigured(
											Throw<Task<int>>(), false),
									AsyncExpression.AwaitConfigured(
											Expression.Constant(Task.FromResult(2)), false))));
			var result = await compiled(Task.FromResult(0));
			Assert.Equal(-1, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestSwitchMixSAAsyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<int>, Task<int>>(debug, paraInput =>
					Expression.Switch(
							AsyncExpression.AwaitConfigured(
									paraInput, false),
							AsyncExpression.AwaitConfigured(
									Expression.Constant(Task.FromResult(-1)), false),
							Expression.SwitchCase(
									AsyncExpression.AwaitConfigured(
											Throw<Task<int>>(), false),
									Expression.Constant(1)),
							Expression.SwitchCase(
									AsyncExpression.AwaitConfigured(
											Throw<Task<int>>(), false),
									AsyncExpression.AwaitConfigured(
											Expression.Constant(Task.FromResult(2)), false))));
			var result = await compiled(Task.FromResult(0));
			Assert.Equal(-1, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestSwitchMixASAsyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<int>, Task<int>>(debug, paraInput =>
					Expression.Switch(
							AsyncExpression.AwaitConfigured(
									paraInput, false),
							AsyncExpression.AwaitConfigured(
									Expression.Constant(Task.FromResult(-1)), false),
							Expression.SwitchCase(
									AsyncExpression.AwaitConfigured(
											Throw<Task<int>>(), false),
									AsyncExpression.AwaitConfigured(
											Expression.Constant(Task.FromResult(1)), false),
									Expression.Constant(4)),
							Expression.SwitchCase(
									AsyncExpression.AwaitConfigured(
											Throw<Task<int>>(), false),
									Expression.Constant(2),
									Expression.Constant(3))));
			var result = await compiled(Task.FromResult(0));
			Assert.Equal(-1, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestOrElseSyncSyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<bool, Task<bool>>(debug, paraInput =>
					Expression.OrElse(
							paraInput,
							Throw<bool>()));
			var result = await compiled(true);
			Assert.True(result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestOrElseAsyncSyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<bool>, Task<bool>>(debug, paraInput =>
					Expression.OrElse(
							AsyncExpression.AwaitConfigured(
									paraInput, false),
							Throw<bool>()));
			var result = await compiled(Task.FromResult(true));
			Assert.True(result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestOrElseSyncAsyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<bool, Task<bool>>(debug, paraInput =>
					Expression.OrElse(
							paraInput,
							AsyncExpression.AwaitConfigured(
									Throw<Task<bool>>(), false)));
			var result = await compiled(true);
			Assert.True(result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestOrElseAsyncAsyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<bool>, Task<bool>>(debug, paraInput =>
					Expression.OrElse(
							AsyncExpression.AwaitConfigured(
									paraInput, false),
							AsyncExpression.AwaitConfigured(
									Throw<Task<bool>>(), false)));
			var result = await compiled(Task.FromResult(true));
			Assert.True(result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestAndAlsoSyncSyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<bool, Task<bool>>(debug, paraInput =>
					Expression.AndAlso(
							paraInput,
							Throw<bool>()));
			var result = await compiled(false);
			Assert.False(result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestAndAlsoAsyncSyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<bool>, Task<bool>>(debug, paraInput =>
					Expression.AndAlso(
							AsyncExpression.AwaitConfigured(
									paraInput, false),
							Throw<bool>()));
			var result = await compiled(Task.FromResult(false));
			Assert.False(result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestAndAlsoSyncAsyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<bool, Task<bool>>(debug, paraInput =>
					Expression.AndAlso(
							paraInput,
							AsyncExpression.AwaitConfigured(
									Throw<Task<bool>>(), false)));
			var result = await compiled(false);
			Assert.False(result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestAndAlsoAsyncAsyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<bool>, Task<bool>>(debug, paraInput =>
					Expression.AndAlso(
							AsyncExpression.AwaitConfigured(
									paraInput, false),
							AsyncExpression.AwaitConfigured(
									Throw<Task<bool>>(), false)));
			var result = await compiled(Task.FromResult(false));
			Assert.False(result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestConditionalSyncSyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<bool>, Task<string>>(debug, paraInput =>
					Expression.Condition(
							AsyncExpression.AwaitConfigured(
									paraInput, false),
							Expression.Constant("success"),
							Throw<string>()));
			var result = await compiled(Task.FromResult(true));
			Assert.Equal("success", result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestConditionalAsyncSyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<bool>, Task<string>>(debug, paraInput =>
					Expression.Condition(
							AsyncExpression.AwaitConfigured(
									paraInput, false),
							AsyncExpression.AwaitConfigured(
									Expression.Constant(Task.FromResult("success")), false),
							Throw<string>()));
			var result = await compiled(Task.FromResult(true));
			Assert.Equal("success", result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestConditionalSyncAsyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<bool>, Task<string>>(debug, paraInput =>
					Expression.Condition(
							AsyncExpression.AwaitConfigured(
									paraInput, false),
							Expression.Constant("success"),
							AsyncExpression.AwaitConfigured(
									Throw<Task<string>>(), false)));
			var result = await compiled(Task.FromResult(true));
			Assert.Equal("success", result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestConditionalAsyncAsyncCompiled(bool debug) {
			var compiled = this.CompileAsyncLambda<Task<bool>, Task<string>>(debug, paraInput =>
					Expression.Condition(
							AsyncExpression.AwaitConfigured(
									paraInput, false),
							AsyncExpression.AwaitConfigured(
									Expression.Constant(Task.FromResult("success")), false),
							AsyncExpression.AwaitConfigured(
									Throw<Task<string>>(), false)));
			var result = await compiled(Task.FromResult(true));
			Assert.Equal("success", result);
		}

		[Fact]
		public void TestInvalidAwait() {
			Assert.Throws<ArgumentException>(() =>
					AsyncExpression.Await(
							Expression.Constant("test")));
		}

		[Fact]
		public void TestInvalidConfigureAwait() {
			Assert.Throws<ArgumentException>(() =>
					AsyncExpression.AwaitConfigured(
							Expression.Constant(Task.Yield()), false));
		}

		[Fact]
		public void TestAwaitType() {
			var exprAwait = AsyncExpression.Await(
					Expression.Constant(Task.FromResult("test")));
			this.Output.WriteLine(exprAwait.ToString(BuiltinRenderer.DebugView));
			Assert.Equal(typeof(string), exprAwait.Type);
		}

		[Fact]
		public void TestAwaitConfiguredType() {
			var exprAwait = AsyncExpression.AwaitConfigured(
					Expression.Constant(Task.FromResult("test")), false);
			this.Output.WriteLine(exprAwait.ToString(BuiltinRenderer.DebugView));
			Assert.Equal(typeof(string), exprAwait.Type);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task TestInLambdaCompiled(bool debug) {
			var meth_AsyncEnumerable_WhereAwait = Reflect.GetStaticMethod(() => AsyncEnumerable.WhereAwait(default, default(Func<int, ValueTask<bool>>)));
			Assert.NotNull(meth_AsyncEnumerable_WhereAwait);
			var paraE = Expression.Parameter(typeof(IAsyncEnumerable<int>), "e");
			var paraI = Expression.Parameter(typeof(int), "i");
			var lambda = Expression.Lambda<Func<IAsyncEnumerable<int>, IAsyncEnumerable<int>>>(
					Expression.Call(meth_AsyncEnumerable_WhereAwait,
							paraE,
							AsyncExpression.AsyncLambda<Func<int, ValueTask<bool>>>(
									Expression.Equal(
											Expression.Modulo(
													paraI,
													AsyncExpression.AwaitConfigured(
															Expression.Constant(Task.FromResult(2)), false)),
											AsyncExpression.AwaitConfigured(
													Expression.Constant(Task.FromResult(0)), false)),
									paraI)),
					paraE);
			var exprAsync = lambda.BuildStateMachines(debug ? DebugInfoGenerator.CreatePdbGenerator() : null);
			this.Output.WriteLine("");
			this.Output.WriteLine("==> Async Lambda");
			this.Output.WriteLine(exprAsync.ToString(BuiltinRenderer.DebugView));
			var compiled = exprAsync.Compile(false);
			var result = await compiled(AsyncEnumerable.Range(1, 3)).ToArrayAsync();
			Assert.Equal<int>(result, new[] { 2 });
		}
	}
}
