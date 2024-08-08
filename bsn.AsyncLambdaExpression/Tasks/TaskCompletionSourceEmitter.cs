using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace bsn.AsyncLambdaExpression.Tasks {
	internal class TaskCompletionSourceEmitter<TResult>: ICompletionSourceEmitter {
		private static readonly ConstructorInfo ctor_TaskCompletionSourceOfTResult = Reflect.GetConstructor(() => new TaskCompletionSource<TResult>(default));
		private static readonly MethodInfo meth_TaskCompletionSourceOfTResult_SetResult = Reflect<TaskCompletionSource<TResult>>.GetMethod(tcs => tcs.SetResult(default));
		private static readonly MethodInfo meth_TaskCompletionSourceOfTResult_SetException = Reflect<TaskCompletionSource<TResult>>.GetMethod(tcs => tcs.SetException(default(Exception)));
		private static readonly PropertyInfo prop_TaskCompletionSourceOfTResult_Task = Reflect<TaskCompletionSource<TResult>>.GetProperty(tcs => tcs.Task);
		internal static readonly MethodInfo meth_Task_FromResult = Reflect.GetStaticMethod(() => Task.FromResult<TResult>(default));
		private static readonly MethodInfo meth_Task_FromException = Reflect.GetStaticMethod(() => Task.FromException<TResult>(default));

		public Expression Create() {
			return Expression.New(ctor_TaskCompletionSourceOfTResult,
					Expression.Constant(TaskCreationOptions.RunContinuationsAsynchronously));
		}

		public virtual Expression SetResult(ParameterExpression varCompletionSource, Expression result) {
			return Expression.Call(varCompletionSource, meth_TaskCompletionSourceOfTResult_SetResult, result);
		}

		public Expression SetException(ParameterExpression varCompletionSource, Expression exception) {
			return Expression.Call(varCompletionSource, meth_TaskCompletionSourceOfTResult_SetException, exception);
		}

		public virtual Expression GetAwaitable(ParameterExpression varCompletionSource) {
			return Expression.Property(varCompletionSource, prop_TaskCompletionSourceOfTResult_Task);
		}

		public virtual Expression GetFromResult(Expression result) {
			return Expression.Call(meth_Task_FromResult, result);
		}

		public virtual Expression GetFromException(Expression exception) {
			return Expression.Call(meth_Task_FromException, exception);
		}
	}

	internal class TaskCompletionSourceEmitter: TaskCompletionSourceEmitter<object> {
		private static readonly PropertyInfo prop_Task_CompletedTask = Reflect.GetStaticProperty(() => Task.CompletedTask);
		private static readonly MethodInfo meth_Task_FromException = Reflect.GetStaticMethod(() => Task.FromException(default));

		public override Expression SetResult(ParameterExpression varCompletionSource, Expression result) {
			return base.SetResult(varCompletionSource, Expression.Default(typeof(object)));
		}

		public override Expression GetAwaitable(ParameterExpression varCompletionSource) {
			return Expression.Convert(base.GetAwaitable(varCompletionSource), typeof(Task));
		}

		public override Expression GetFromResult(Expression result) {
			return Expression.Block(
					result,
					Expression.Property(null, prop_Task_CompletedTask));
		}

		public override Expression GetFromException(Expression exception) {
			return Expression.Call(meth_Task_FromException, exception);
		}
	}
}
