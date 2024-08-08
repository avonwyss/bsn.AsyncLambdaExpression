using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace bsn.AsyncLambdaExpression.Tasks {
	internal class ValueTaskCompletionSourceEmitter<TResult>: ICompletionSourceEmitter {
		private static readonly ConstructorInfo ctor_ValueTaskCompletionSourceOfTResult = Reflect.GetConstructor(() => new ValueTaskCompletionSource<TResult>());
		private static readonly MethodInfo meth_ValueTaskCompletionSourceOfTResult_SetResult = Reflect<ValueTaskCompletionSource<TResult>>.GetMethod(vtcs => vtcs.SetResult(default));
		private static readonly MethodInfo meth_ValueTaskCompletionSourceOfTResult_SetException = Reflect<ValueTaskCompletionSource<TResult>>.GetMethod(vtcs => vtcs.SetException(default));
		private static readonly MethodInfo meth_ValueTaskCompletionSourceOfTResult_GetValueTask = Reflect<ValueTaskCompletionSource<TResult>>.GetMethod(vtcs => vtcs.GetValueTask());
		private static readonly ConstructorInfo ctor_ValueTaskOfTResult_Result = Reflect.GetConstructor(() => new ValueTask<TResult>(default(TResult)));
		private static readonly ConstructorInfo ctor_ValueTaskOfTResult_Task = Reflect.GetConstructor(() => new ValueTask<TResult>(default(Task<TResult>)));
		private static readonly MethodInfo meth_Task_FromExceptionOfTResult = Reflect.GetStaticMethod(() => Task.FromException<TResult>(default));

		public Expression Create() {
			return Expression.New(ctor_ValueTaskCompletionSourceOfTResult);
		}

		public Expression SetResult(ParameterExpression varCompletionSource, Expression result) {
			return Expression.Call(varCompletionSource, meth_ValueTaskCompletionSourceOfTResult_SetResult, result);
		}

		public Expression SetException(ParameterExpression varCompletionSource, Expression exception) {
			return Expression.Call(varCompletionSource, meth_ValueTaskCompletionSourceOfTResult_SetException, exception);
		}

		public Expression GetAwaitable(ParameterExpression varCompletionSource) {
			return Expression.Call(varCompletionSource, meth_ValueTaskCompletionSourceOfTResult_GetValueTask);
		}

		public Expression GetFromResult(Expression result) {
			return Expression.New(ctor_ValueTaskOfTResult_Result, result);
		}

		public Expression GetFromException(Expression exception) {
			return Expression.New(ctor_ValueTaskOfTResult_Task,
					Expression.Call(meth_Task_FromExceptionOfTResult, exception));
		}
	}

	internal class ValueTaskCompletionSourceEmitter: ICompletionSourceEmitter {
		private static readonly ConstructorInfo ctor_ValueTaskCompletionSource = Reflect.GetConstructor(() => new ValueTaskCompletionSource());
		private static readonly MethodInfo meth_ValueTaskCompletionSource_SetResult = Reflect<ValueTaskCompletionSource>.GetMethod(vtcs => vtcs.SetResult());
		private static readonly MethodInfo meth_ValueTaskCompletionSource_SetException = Reflect<ValueTaskCompletionSource>.GetMethod(vtcs => vtcs.SetException(default));
		private static readonly MethodInfo meth_ValueTaskCompletionSource_GetValueTask = Reflect<ValueTaskCompletionSource>.GetMethod(vtcs => vtcs.GetValueTask());
		private static readonly ConstructorInfo ctor_ValueTask_Task = Reflect.GetConstructor(() => new ValueTask(default));
		private static readonly MethodInfo meth_Task_FromException = Reflect.GetStaticMethod(() => Task.FromException<object>(default));

		public Expression Create() {
			return Expression.New(ctor_ValueTaskCompletionSource);
		}

		public Expression SetResult(ParameterExpression varCompletionSource, Expression result) {
			return Expression.Call(varCompletionSource, meth_ValueTaskCompletionSource_SetResult);
		}

		public Expression SetException(ParameterExpression varCompletionSource, Expression exception) {
			return Expression.Call(varCompletionSource, meth_ValueTaskCompletionSource_SetException, exception);
		}

		public Expression GetAwaitable(ParameterExpression varCompletionSource) {
			return Expression.Call(varCompletionSource, meth_ValueTaskCompletionSource_GetValueTask);
		}

		public Expression GetFromResult(Expression result) {
			return Expression.Block(
					result,
					Expression.Default(typeof(ValueTask)));
		}

		public Expression GetFromException(Expression exception) {
			return Expression.New(ctor_ValueTask_Task,
					Expression.Call(meth_Task_FromException, exception));
		}
	}
}
