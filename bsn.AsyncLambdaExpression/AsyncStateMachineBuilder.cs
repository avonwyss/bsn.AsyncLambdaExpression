using System;
using System.CodeDom;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using bsn.AsyncLambdaExpression.Expressions;

namespace bsn.AsyncLambdaExpression {
	internal partial class AsyncStateMachineBuilder {
		private static readonly ConcurrentDictionary<Type, (ConstructorInfo, MethodInfo, MethodInfo, PropertyInfo)> taskCompletionSourceInfos = new();
		private static readonly ConstructorInfo ctor_InvalidOperationExpression = typeof(InvalidOperationException).GetConstructor(Type.EmptyTypes);
		private static readonly PropertyInfo prop_Task_CompletedTask = typeof(Task).GetProperty(nameof(Task.CompletedTask));
		private static readonly MethodInfo meth_Task_FromResultOfType = typeof(Task).GetMethod(nameof(Task.FromResult));

		internal static (ConstructorInfo ctor, MethodInfo methSetResult, MethodInfo methSetException, PropertyInfo propTask) GetTaskCompletionSourceInfo(Type type) {
			return taskCompletionSourceInfos.GetOrAdd(type, static t => (
					t.GetConstructor(new[] { typeof(TaskCreationOptions) }),
					t.GetMethod(nameof(TaskCompletionSource<object>.SetResult)),
					t.GetMethod(nameof(TaskCompletionSource<object>.SetException), new[] { typeof(Exception) }),
					t.GetProperty(nameof(TaskCompletionSource<object>.Task))
			));
		}

		private readonly ConcurrentDictionary<Type, ParameterExpression> varAwaiter = new();

		internal ParameterExpression VarState {
			get;
		}

		internal ParameterExpression VarTaskCompletionSource {
			get;
		}

		internal ParameterExpression VarContinuation {
			get;
		}

		internal LabelTarget LblBreak {
			get;
		}

		public LambdaExpression Lambda {
			get;
		}

		public Type ResultTaskType {
			get;
		}

		internal ParameterExpression GetVarAwaiter(Type awaiterType) {
			Debug.Assert(awaiterType.IsAwaiter());
			return varAwaiter.GetOrAdd(awaiterType, static t => Expression.Variable(t, "awaiter"));
		}

		public AsyncStateMachineBuilder(LambdaExpression lambda, Type resultTaskType) {
			if (!resultTaskType.IsTask()) {
				throw new ArgumentException("Only Task<> and Task are supported as return types");
			}
			Lambda = lambda;
			ResultTaskType = resultTaskType;
			VarState = Expression.Variable(typeof(int), "state");
			VarTaskCompletionSource = Expression.Variable(typeof(TaskCompletionSource<>).MakeGenericType(ResultTaskType.GetAsyncReturnType() ?? typeof(object)), "taskCompletionSource");
			VarContinuation = Expression.Variable(typeof(Action), "continuation");
			LblBreak = Expression.Label(typeof(void), ":break");
		}

		public Expression CreateStateMachineBody() {
			var (ctor_TaskCompletionSource, meth_TaskCompletionSource_SetResult, meth_TaskCompletionSource_SetException, prop_TaskCompletionSource_Task) = GetTaskCompletionSourceInfo(VarTaskCompletionSource.Type);
			var varException = Expression.Variable(typeof(Exception), "ex");
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
					.Append(VarTaskCompletionSource)
					.Append(VarContinuation).ToList();
			return Expression.Block(ResultTaskType, variables,
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
																			state.ToExpression(VarState),
																			Expression.Constant(state.StateId)))),
													LblBreak),
											Expression.Catch(varException,
													Expression.Call(VarTaskCompletionSource, meth_TaskCompletionSource_SetException, varException))))),
							Expression.Invoke(VarContinuation),
							Expression.Property(VarTaskCompletionSource, prop_TaskCompletionSource_Task))
					.Optimize()
					.RescopeVariables(Lambda.Parameters.Concat(variables)); // 2nd optimize pass cleans up variable-related stuff
		}

		public static bool IsAwaitExpression(Expression node) {
			return node is MethodCallExpression callNode && AsyncExpressionExtensions.IsAwaitMethod(callNode.Method);
		}
	}
}
