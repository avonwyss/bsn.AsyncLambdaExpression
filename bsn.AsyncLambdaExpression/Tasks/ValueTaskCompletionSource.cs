using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace bsn.AsyncLambdaExpression.Tasks {
	public class ValueTaskCompletionSource: IValueTaskSource {
		// Don't make this readonly, since it is a mutable struct
		private ManualResetValueTaskSourceCore<object> mrvts;

		public ValueTaskCompletionSource() {
			this.mrvts.RunContinuationsAsynchronously = true;
		}

		public void SetResult() {
			this.mrvts.SetResult(default);
		}

		public void SetException(Exception error) {
			this.mrvts.SetException(error);
		}

		public ValueTask GetValueTask() {
			return new ValueTask(this, this.mrvts.Version);
		}

		void IValueTaskSource.GetResult(short token) {
			this.mrvts.GetResult(token);
		}

		ValueTaskSourceStatus IValueTaskSource.GetStatus(short token) {
			return this.mrvts.GetStatus(token);
		}

		void IValueTaskSource.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags) {
			this.mrvts.OnCompleted(continuation, state, token, flags);
		}
	}

	public class ValueTaskCompletionSource<TResult>: IValueTaskSource<TResult> {
		// Don't make this readonly, since it is a mutable struct
		private ManualResetValueTaskSourceCore<TResult> mrvts;

		public ValueTaskCompletionSource() {
			this.mrvts.RunContinuationsAsynchronously = true;
		}

		public void SetResult(TResult result) {
			this.mrvts.SetResult(result);
		}

		public void SetException(Exception error) {
			this.mrvts.SetException(error);
		}

		public ValueTask<TResult> GetValueTask() {
			return new ValueTask<TResult>(this, this.mrvts.Version);
		}

		TResult IValueTaskSource<TResult>.GetResult(short token) {
			return this.mrvts.GetResult(token);
		}

		ValueTaskSourceStatus IValueTaskSource<TResult>.GetStatus(short token) {
			return this.mrvts.GetStatus(token);
		}

		void IValueTaskSource<TResult>.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags) {
			this.mrvts.OnCompleted(continuation, state, token, flags);
		}
	}
}
