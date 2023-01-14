using System;
using System.CodeDom;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace bsn.AsyncLambdaExpression {
	internal partial class AsyncStateMachineBuilder {
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
		private readonly ParameterExpression varState;
		private readonly ParameterExpression varTaskCompletionSource;
		private readonly ParameterExpression varContinuation;
		private readonly LabelTarget lblBreak;

		public LambdaExpression Lambda {
			get;
		}

		public Type ResultType {
			get;
		}

		internal ParameterExpression GetVarAwaiter(Type awaiterType) {
			Debug.Assert(awaiterType.IsAwaiter());
			return varAwaiter.GetOrAdd(awaiterType, static t => Expression.Variable(t, "awaiter"));
		}

		public AsyncStateMachineBuilder(LambdaExpression lambda, Type resultType) {
			if (!resultType.IsTask()) {
				throw new ArgumentException("Only Task<> and Task are supported as return types");
			}
			Lambda = lambda;
			ResultType = resultType;
			varState = Expression.Variable(typeof(int), "state");
			varTaskCompletionSource = Expression.Variable(typeof(TaskCompletionSource<>).MakeGenericType(ResultType.GetAsyncReturnType() ?? typeof(object)), "taskCompletionSource");
			varContinuation = Expression.Variable(typeof(Action), "continuation");
			lblBreak = Expression.Label(typeof(void), ":break");
		}

		public Expression CreateStateMachineBody() {
			var (ctor_TaskCompletionSource, _, meth_TaskCompletionSource_SetException, prop_TaskCompletionSource_Task) = GetTaskCompletionSourceInfo(varTaskCompletionSource.Type);
			var varException = Expression.Variable(typeof(Exception), "ex");
			var continuationBuilder = new ContinuationBuilder(this);
			var finalState = continuationBuilder.Process(Lambda.Body);
			if (finalState.StateId == 0) {
				return (ResultType == typeof(Task)
								? (Expression)Expression.Block(
										Lambda.Body,
										Expression.Property(null, prop_Task_CompletedTask)
								)
								: Expression.Call(meth_Task_FromResultOfType.MakeGenericMethod(Lambda.Body.Type), Lambda.Body))
						.Optimize();
			}
			finalState.AddExpression(Expression.Break(lblBreak));
			var variables = varAwaiter.Values
					.Append(varState)
					.Append(varTaskCompletionSource)
					.Append(varContinuation).ToList();
			return Expression.Block(ResultType, variables,
							Expression.Assign(
									varTaskCompletionSource,
									Expression.New(ctor_TaskCompletionSource,
											Expression.Constant(TaskCreationOptions.RunContinuationsAsynchronously))),
							Expression.Assign(varContinuation,
									Expression.Lambda<Action>(Expression.TryCatch(
											Expression.Loop(
													Expression.Switch(typeof(void),
															varState,
															Expression.Throw(
																	Expression.New(ctor_InvalidOperationExpression)),
															null,
															continuationBuilder.States.Select(state =>
																	Expression.SwitchCase(
																			state.ToExpression(varState),
																			Expression.Constant(state.StateId)))),
													lblBreak),
											Expression.Catch(varException,
													Expression.Call(varTaskCompletionSource, meth_TaskCompletionSource_SetException, varException))))),
							Expression.Invoke(varContinuation),
							Expression.Property(varTaskCompletionSource, prop_TaskCompletionSource_Task))
					.Optimize()
					.RescopeVariables(Lambda.Parameters.Concat(variables)); // 2nd optimize pass cleans up variable-related stuff
		}

		public static bool IsAwaitExpression(Expression node) {
			return node is MethodCallExpression callNode && AsyncExpressionExtensions.IsAwaitMethod(callNode.Method);
		}
	}
}
