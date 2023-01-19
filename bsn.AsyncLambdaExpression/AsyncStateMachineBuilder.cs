using System;
using System.CodeDom;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using bsn.AsyncLambdaExpression.Collections;
using bsn.AsyncLambdaExpression.Expressions;

namespace bsn.AsyncLambdaExpression {
	internal partial class AsyncStateMachineBuilder: IAsyncStateMachineVariables {
		private static readonly ConcurrentDictionary<Type, (ConstructorInfo, MethodInfo, MethodInfo, PropertyInfo)> taskCompletionSourceInfos = new();
		private static readonly ConstructorInfo ctor_InvalidOperationExpression = typeof(InvalidOperationException).GetConstructor(Type.EmptyTypes);
		private static readonly PropertyInfo prop_Task_CompletedTask = typeof(Task).GetProperty(nameof(Task.CompletedTask));
		private static readonly MethodInfo meth_Task_FromResultOfType = typeof(Task).GetMethod(nameof(Task.FromResult));

		private static (ConstructorInfo ctor, MethodInfo methSetResult, MethodInfo methSetException, PropertyInfo propTask) GetTaskCompletionSourceInfo(Type type) {
			return taskCompletionSourceInfos.GetOrAdd(type, static t => (
					t.GetConstructor(new[] { typeof(TaskCreationOptions) }),
					t.GetMethod(nameof(TaskCompletionSource<object>.SetResult)),
					t.GetMethod(nameof(TaskCompletionSource<object>.SetException), new[] { typeof(Exception) }),
					t.GetProperty(nameof(TaskCompletionSource<object>.Task))
			));
		}

		private readonly ConcurrentDictionary<Type, ParameterExpression> varAwaiter = new();

		public ParameterExpression VarException {
			get;
		}

		public ParameterExpression VarResumeState {
			get;
		}

		public ParameterExpression VarState {
			get;
		}

		private ParameterExpression VarTaskCompletionSource {
			get;
		}

		public ParameterExpression VarContinuation {
			get;
		}

		public LabelTarget LblBreak {
			get;
		}

		private LambdaExpression Lambda {
			get;
		}

		private Type ResultTaskType {
			get;
		}

		public ParameterExpression GetVarAwaiter(Type awaiterType) {
			Debug.Assert(awaiterType.IsAwaiter());
			return varAwaiter.GetOrAdd(awaiterType, static t => Expression.Variable(t, "awaiter"));
		}

		public Expression GetSetExceptionCall(ParameterExpression varException) {
			var meth_TaskCompletionSource_SetException = GetTaskCompletionSourceInfo(VarTaskCompletionSource.Type).methSetException;
			return Expression.Call(VarTaskCompletionSource, meth_TaskCompletionSource_SetException, varException);
		}

		public AsyncStateMachineBuilder(LambdaExpression lambda, Type resultTaskType) {
			if (!resultTaskType.IsTask()) {
				throw new ArgumentException("Only Task<> and Task are supported as return types");
			}
			Lambda = lambda;
			ResultTaskType = resultTaskType;
			VarState = Expression.Variable(typeof(int), "state");
			VarResumeState = Expression.Variable(typeof(int), "resumeState");
			VarTaskCompletionSource = Expression.Variable(typeof(TaskCompletionSource<>).MakeGenericType(ResultTaskType.GetAsyncReturnType() ?? typeof(object)), "taskCompletionSource");
			VarContinuation = Expression.Variable(typeof(Action), "continuation");
			VarException = Expression.Variable(typeof(Exception), "exception");
			LblBreak = Expression.Label(typeof(void), ":break");
		}

		public Expression CreateStateMachineBody(bool debug) {
			var (ctor_TaskCompletionSource, meth_TaskCompletionSource_SetResult, meth_TaskCompletionSource_SetException, prop_TaskCompletionSource_Task) = GetTaskCompletionSourceInfo(VarTaskCompletionSource.Type);
			var varEx = Expression.Variable(typeof(Exception), "ex");
			var continuationBuilder = new ContinuationBuilder(this);
			var (finalState, finalExpr) = continuationBuilder.Process(Lambda.Body);
			if (finalState.StateId == 0) {
				// Nothing async, just wrap into a Task
				// TODO: Be a good citizen: catch exception and pass it to Task.FromException()
				return (ResultTaskType == typeof(Task)
								? (Expression)Expression.Block(
										Lambda.Body,
										Expression.Property(null, prop_Task_CompletedTask)
								)
								: Expression.Call(meth_Task_FromResultOfType.MakeGenericMethod(Lambda.Body.Type), Lambda.Body))
						.Optimize();
			}
			if (ResultTaskType == typeof(object)) {
				finalState.AddExpression(finalExpr);
			}
			finalState.AddExpression(
					Expression.Call(VarTaskCompletionSource, meth_TaskCompletionSource_SetResult,
							ResultTaskType == typeof(object)
									? Expression.Default(typeof(object))
									: finalExpr));
			finalState.AddExpression(
					Expression.Break(LblBreak));
			var variables = varAwaiter.Values
					.Append(VarState)
					.Append(VarResumeState)
					.Append(VarTaskCompletionSource)
					.Append(VarContinuation)
					.Append(VarException)
					.ToList();
			Expression stateMachine = Expression.Block(ResultTaskType, variables,
					Expression.Assign(
							VarTaskCompletionSource,
							Expression.New(ctor_TaskCompletionSource,
									Expression.Constant(TaskCreationOptions.RunContinuationsAsynchronously))),
					Expression.Assign(VarContinuation,
							Expression.Lambda<Action>(Expression.TryCatch(
									Expression.Loop(
											Expression.Switch(typeof(void),
													VarState,
													Expression.Throw(
															Expression.New(ctor_InvalidOperationExpression)),
													null,
													continuationBuilder.States.Select(state =>
															Expression.SwitchCase(
																	debug ? StateBodyExpressionDebug(state) : StateBodyExpression(state),
																	Expression.Constant(state.StateId)))),
											LblBreak),
									Expression.Catch(varEx,
											Expression.Call(VarTaskCompletionSource, meth_TaskCompletionSource_SetException, varEx))))),
					Expression.Invoke(VarContinuation),
					Expression.Property(VarTaskCompletionSource, prop_TaskCompletionSource_Task));
			if (!debug) {
				stateMachine = stateMachine.Optimize();
			}
			return stateMachine.RescopeVariables(Lambda.Parameters.Concat(variables));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Expression StateBodyExpressionDebug(AsyncState state) {
			var result = StateBodyExpression(state);
#if DEBUG
			if (!string.IsNullOrEmpty(state.Name)) {
				return Expression.Block(
						Expression.Constant(state.Name),
						result);
			}
#endif
			return result;
		}

		private Expression StateBodyExpression(AsyncState state) {
			var bodyExpression = state.ToExpression(this);
			if (state.TryInfos.IsEmpty) {
				return bodyExpression;
			}
			var finallyState = state.TryInfos.Peek().FinallyState;
			var rethrowState = state.TryInfos.Peek().RethrowState;
			var catchBlocks = new List<CatchBlock>();
			var catchBody = new List<Expression>();
			var varEx = default(ParameterExpression);
			var exceptionTypes = new TypeAssignableSet();
			for (var tryInfo = state.TryInfos; !tryInfo.IsEmpty; tryInfo = tryInfo.Pop()) {
				foreach (var handler in tryInfo.Peek().Handlers) {
					if (handler.Filter != null || exceptionTypes.Add(handler.Test)) {
						varEx = Expression.Variable(handler.Test, "ex");
						catchBody.Clear();
						catchBody.Add(Expression.Assign(VarException, varEx));
						if (handler.Variable != null) {
							catchBody.Add(Expression.Assign(handler.Variable, varEx));
						}
						if (tryInfo == state.TryInfos || finallyState == null) {
							catchBody.Add(Expression.Assign(VarState, Expression.Constant(handler.BodyState.StateId)));
						} else {
							catchBody.Add(Expression.Assign(VarResumeState, Expression.Constant(handler.BodyState.StateId)));
							catchBody.Add(Expression.Assign(VarState, Expression.Constant(finallyState.StateId)));
						}
						catchBlocks.Add(Expression.Catch(varEx, Expression.Block(catchBody), handler.Filter));
					}
				}
			}
			if (!exceptionTypes.Contains(typeof(Exception))) {
				varEx = Expression.Variable(typeof(Exception), "ex");
				catchBody.Clear();
				catchBody.Add(Expression.Assign(VarException, varEx));
				if (state.TryInfos.TryFirstNotNull(ti => ti.FinallyState, out finallyState)) {
					catchBody.Add(Expression.Assign(VarResumeState, Expression.Constant(rethrowState.StateId)));
					catchBody.Add(Expression.Assign(VarState, Expression.Constant(finallyState.StateId)));
				} else {
					catchBody.Add(Expression.Assign(VarState, Expression.Constant(rethrowState.StateId)));
				}
				catchBlocks.Add(Expression.Catch(varEx, Expression.Block(catchBody)));
			}
			return Expression.MakeTry(typeof(void), bodyExpression, null, null, catchBlocks);
		}

		public static bool IsAwaitExpression(Expression node) {
			return node is MethodCallExpression callNode && AsyncExpressionExtensions.IsAwaitMethod(callNode.Method);
		}
	}
}
