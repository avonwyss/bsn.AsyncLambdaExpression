using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using bsn.AsyncLambdaExpression.Expressions;

namespace bsn.AsyncLambdaExpression {
	internal class AsyncStateMachineBuilder: StateMachineBuilderBase {
		public override Expression CreateStateMachineBody(bool debug) {
			var (ctor_TaskCompletionSource, meth_TaskCompletionSource_SetResult, meth_TaskCompletionSource_SetException, prop_TaskCompletionSource_Task) = GetTaskCompletionSourceInfo(this.VarTaskCompletionSource.Type);
			var varEx = Expression.Variable(typeof(Exception), "ex");
			var continuationBuilder = new ContinuationBuilder(this);
			var (finalState, finalExpr) = continuationBuilder.Process(this.Lambda.Body);
			var voidResult = this.ResultType == typeof(Task) || this.ResultType == typeof(ValueTask);
			if (finalState.StateId == 0) {
				// Nothing async, just wrap into a Task or ValueTask
				return (Reflect.IsValueTask(this.ResultType)
								? voidResult
										? Expression.TryCatch( // return ValueTask
												Expression.Block(
														this.Lambda.Body,
														Expression.Default(typeof(ValueTask))),
												Expression.Catch(varEx,
														Expression.New(ctor_ValueTask_Task,
																Expression.Call(meth_Task_FromException, varEx))))
										: Expression.TryCatch( // return ValueTask<T>
												Expression.New(GetValueTaskInfo(this.ResultType).ctor_Value,
														this.Lambda.Body),
												Expression.Catch(varEx,
														Expression.New(GetValueTaskInfo(this.ResultType).ctor_Task,
																Expression.Call(meth_Task_FromExceptionOfType.MakeGenericMethod(this.Lambda.Body.Type),
																		varEx))))
								: voidResult
										? Expression.TryCatch( // return Task
												Expression.Block(
														this.Lambda.Body,
														Expression.Property(null, prop_Task_CompletedTask)),
												Expression.Catch(varEx,
														Expression.Call(meth_Task_FromException, varEx)))
										: Expression.TryCatch( // return Task<T>
												Expression.Call(meth_Task_FromResultOfType.MakeGenericMethod(this.Lambda.Body.Type),
														this.Lambda.Body),
												Expression.Catch(varEx,
														Expression.Call(meth_Task_FromExceptionOfType.MakeGenericMethod(this.Lambda.Body.Type),
																varEx))))
						.Optimize();
			}
			if (voidResult) {
				finalState.AddExpression(finalExpr);
			}
			finalState.AddExpression(
					Expression.Call(this.VarTaskCompletionSource, meth_TaskCompletionSource_SetResult, voidResult
							? Expression.Default(typeof(object))
							: finalExpr));
			finalState.AddExpression(
					Expression.Break(this.LblBreak));
			var variables = this.GetVars().Where(v => v != null).ToList();
			Expression stateMachine = Expression.Block(this.ResultType, variables,
					Expression.Assign(this.VarTaskCompletionSource,
							Expression.New(ctor_TaskCompletionSource,
									Expression.Constant(TaskCreationOptions.RunContinuationsAsynchronously))),
					Expression.Assign(this.VarContinuation,
							Expression.Lambda<Action>(Expression.TryCatch(
									Expression.Loop(
											Expression.Switch(typeof(void), this.VarState,
													Expression.Throw(
															Expression.New(ctor_InvalidOperationExpression)),
													null,
													continuationBuilder.States.Select(state =>
															Expression.SwitchCase(
																	this.StateBodyExpressionDebug(state, null, debug),
																	Expression.Constant(state.StateId)))), this.LblBreak),
									Expression.Catch(varEx,
											Expression.Call(this.VarTaskCompletionSource, meth_TaskCompletionSource_SetException, varEx))))),
					Expression.Invoke(this.VarContinuation),
					Reflect.IsValueTask(this.ResultType)
							? Expression.New(voidResult ? ctor_ValueTask_Task : GetValueTaskInfo(this.ResultType).ctor_Task,
									Expression.Property(this.VarTaskCompletionSource, prop_TaskCompletionSource_Task))
							: Expression.Property(this.VarTaskCompletionSource, prop_TaskCompletionSource_Task));
			if (!debug) {
				stateMachine = stateMachine.Optimize();
			}
			return stateMachine.RescopeVariables(this.Lambda.Parameters.Concat(variables));
		}

		public override Expression HandleException(ParameterExpression varException) {
			var meth_TaskCompletionSource_SetException = GetTaskCompletionSourceInfo(this.VarTaskCompletionSource.Type).meth_SetException;
			return Expression.Call(this.VarTaskCompletionSource, meth_TaskCompletionSource_SetException, varException);
		}

		public AsyncStateMachineBuilder(LambdaExpression lambda, Type resultType): base(lambda, resultType, typeof(void)) {
			if (!resultType.IsTask() && !resultType.IsValueTask()) {
				throw new ArgumentException("Only Task<>, Task, ValueTask<> and ValueTask are supported as return types");
			}
			this.VarTaskCompletionSource = Expression.Variable(typeof(TaskCompletionSource<>).MakeGenericType(Reflect.GetAsyncReturnType(this.ResultType) ?? typeof(object)), "taskCompletionSource");
			this.VarContinuation = Expression.Variable(typeof(Action), "continuation");
		}
	}
}
