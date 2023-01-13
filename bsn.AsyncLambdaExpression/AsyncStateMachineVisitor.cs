using System;
using System.CodeDom;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

using Seram.Web;

namespace bsn.AsyncLambdaExpression {
	internal partial class AsyncStateMachineBuilder {
		private class ReorganizeVisitor: ExpressionVisitor {
			private readonly AsyncStateMachineBuilder builder;
			internal readonly Dictionary<Expression, AsyncState> expressionStates = new(ReferenceEqualityComparer<Expression>.Default);
			internal readonly List<AsyncState> states = new();
			private AsyncState currentState;

			public ReorganizeVisitor(AsyncStateMachineBuilder builder) {
				this.builder = builder;
			}

			protected override Expression VisitBlock(BlockExpression node) {
				return base.VisitBlock(node);
			}

			protected override Expression VisitMethodCall(MethodCallExpression node) {
				if (IsAwaitExpression(node)) {
					var nextState = CreateState(node.Type);
					currentState.SetContinuation(nextState);
					var exprAwaitable = Visit(node.Arguments[0]);
					var varAwaiter = builder.GetVarAwaiter(exprAwaitable.Type.GetAwaitableGetAwaiterMethod().ReturnType);
					currentState.AddExpression(
							Expression.IfThen(
									Expression.Not(
											Expression.Property(
													Expression.Assign(
															varAwaiter,
															Expression.Call(exprAwaitable, exprAwaitable.Type.GetAwaitableGetAwaiterMethod())),
													varAwaiter.Type.GetAwaiterIsCompletedProperty())),
									Expression.Block(
											Expression.Call(
													varAwaiter,
													varAwaiter.Type.GetAwaiterOnCompletedMethod(),
													builder.varNext),
											Expression.Break(builder.lblBreak))));
					nextState.AddExpression(varAwaiter.Type.GetAwaiterGetResultMethod().ReturnType == typeof(void)
							? Expression.Call(varAwaiter, varAwaiter.Type.GetAwaiterGetResultMethod())
							: Expression.Assign(nextState.ResultExpression, Expression.Call(varAwaiter, varAwaiter.Type.GetAwaiterGetResultMethod())));
					currentState = nextState;
					return currentState.ResultExpression;
				}
				return base.VisitMethodCall(node);
			}

			private AsyncState CreateState(Type result) {
				var state = new AsyncState(states.Count, result);
				states.Add(state);
				return state;
			}

			public override Expression Visit(Expression node) {
				if (node == null) {
					return null;
				}
				expressionStates[node] = currentState;
				var newNode = base.Visit(node);
				expressionStates[newNode] = currentState;
				return newNode;
			}

			public AsyncState Process(Expression node) {
				expressionStates.Clear();
				states.Clear();
				currentState = CreateState(typeof(void));
				var exprEnd = Visit(node);
				currentState.AddExpression(exprEnd);
				return currentState;
			}
		}

		/*
				private class OldAsyncState: ExpressionVisitor {
					private readonly AsyncStateMachineBuilder builder;
					private readonly AsyncState previous;
		
					private readonly List<Expression> expressions = new();
		
					public AsyncState(AsyncStateMachineBuilder builder, AsyncState previous) {
						this.StateId = builder.fragments.Count;
						builder.fragments.Add(this);
						this.builder = builder;
						this.previous = previous;
					}
		
					public int StateId {
						get;
					}
		
					public void AddExpression(Expression expression) {
						expressions.Add(expression);
					}
		
					public Expression ToExpression() {
					}
		
					protected override Expression VisitBlock(BlockExpression node) {
						return base.VisitBlock(node);
					}
		
					protected override Expression VisitMethodCall(MethodCallExpression node) {
						if (IsAwaitExpression(node)) {
							var exprAwaitable = node.Arguments[0];
							var varAwaiter = this.varAwaiter.GetOrAdd(exprAwaitable.Type.GetAwaitableGetAwaiterMethod().ReturnType, static t => Expression.Variable(t, "awaiter"));
							fragments.Add(Expression.Block(
									Expression.Assign(varState, Expression.Constant(fragments.Count + 1)),
									Expression.IfThen(
											Expression.Not(
													Expression.Property(
															Expression.Assign(
																	varAwaiter,
																	Expression.Call(exprAwaitable, exprAwaitable.Type.GetAwaitableGetAwaiterMethod())),
															varAwaiter.Type.GetAwaiterIsCompletedProperty())),
											Expression.Block(
													Expression.Call(
															varAwaiter,
															varAwaiter.Type.GetAwaiterOnCompletedMethod(),
															varNext),
													Expression.Break(lblBreak)))));
							return Expression.Call(varAwaiter, varAwaiter.Type.GetAwaiterGetResultMethod());
						}
						return base.VisitMethodCall(node);
					}
				}
		*/
		private static readonly ConcurrentDictionary<Type, (ConstructorInfo, MethodInfo, MethodInfo, PropertyInfo)> taskCompletionSourceInfos = new();
		private static readonly ConstructorInfo ctor_InvalidOperationExpression = typeof(InvalidOperationException).GetConstructor(Type.EmptyTypes);

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
		private readonly ParameterExpression varNext;
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
			varNext = Expression.Variable(typeof(Action), "next");
			lblBreak = Expression.Label(typeof(void), "$break");
		}

		public Expression CreateStateMachineBody() {
			var (ctor_TaskCompletionSource, meth_TaskCompletionSource_SetResult, meth_TaskCompletionSource_SetException, prop_TaskCompletionSource_Task) = GetTaskCompletionSourceInfo(varTaskCompletionSource.Type);
			var varException = Expression.Variable(typeof(Exception), "ex");
			var visitor = new ReorganizeVisitor(this);
			var finalState = visitor.Process(Lambda.Body);
			var taskCompletionSourceResultType = meth_TaskCompletionSource_SetResult.GetParameters()[0].ParameterType;
			finalState.AddExpression(
					Expression.Call(varTaskCompletionSource, meth_TaskCompletionSource_SetResult, ResultType == typeof(Task)
							? Expression.Block(finalState.ResultExpression, Expression.Default(taskCompletionSourceResultType))
							: taskCompletionSourceResultType != finalState.ResultExpression.Type
									? Expression.Convert(finalState.ResultExpression, taskCompletionSourceResultType)
									: finalState.ResultExpression));
			finalState.AddExpression(
					Expression.Break(lblBreak));
			return new OptimizeVisitor().Visit(
					Expression.Block(ResultType, varAwaiter.Values.Append(varState).Append(varTaskCompletionSource).Append(varNext),
							Expression.Assign(
									varTaskCompletionSource,
									Expression.New(ctor_TaskCompletionSource,
											Expression.Constant(TaskCreationOptions.RunContinuationsAsynchronously))),
							Expression.Assign(varNext,
									Expression.Lambda<Action>(Expression.TryCatch(
											Expression.Loop(
													Expression.Switch(typeof(void),
															varState,
															Expression.Throw(
																	Expression.New(ctor_InvalidOperationExpression)),
															null,
															visitor.states.Select(state =>
																	Expression.SwitchCase(
																			state.ToExpression(varState),
																			Expression.Constant(state.StateId)))),
													lblBreak),
											Expression.Catch(varException,
													Expression.Call(varTaskCompletionSource, meth_TaskCompletionSource_SetException, varException))))),
							Expression.Invoke(varNext),
							Expression.Property(varTaskCompletionSource, prop_TaskCompletionSource_Task)));
		}

		public static bool IsAwaitExpression(Expression node) {
			return node is MethodCallExpression callNode && AsyncExpressionExtensions.IsAwaitMethod(callNode.Method);
		}
	}

	internal class AsyncState {
		private readonly List<Expression> expressions = new(1);

		public int StateId {
			get;
		}

		public Expression ResultExpression {
			get;
		}

		public AsyncState Continuation {
			get;
			private set;
		}

		public AsyncState(int stateId, Type result) {
			StateId = stateId;
			ResultExpression = result == null || result == typeof(void)
					? Expression.Empty()
					: Expression.Variable(result);
		}

		public void SetContinuation(AsyncState state) {
			Debug.Assert(Continuation == null);
			Continuation = state;
		}

		public void AddExpression(Expression expression) {
			expressions.Add(expression);
		}

		public Expression ToExpression(ParameterExpression varState) {
			var result = Expression.Block(ResultExpression is ParameterExpression varResult ? varResult.Yield() : Enumerable.Empty<ParameterExpression>(),
					expressions.Prepend(Expression.Assign(
							varState,
							Expression.Constant(Continuation?.StateId ?? -1))));
			return result.Expressions.Count == 1 ? result.Expressions[0] : result;
		}

		public override string ToString() {
			return Continuation == null ? StateId.ToString() : $"{StateId} => {Continuation.StateId}";
		}
	}
}
