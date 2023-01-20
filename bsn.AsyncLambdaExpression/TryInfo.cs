using System;
using System.Diagnostics;
using System.Linq;

namespace bsn.AsyncLambdaExpression {
	internal struct TryInfo {
		public TryInfo(CatchInfo[] handlers, AsyncState finallyState, AsyncState rethrowState, AsyncState exitState) {
			Debug.Assert(handlers == null || handlers.All(c => c.BodyState.StateId > 0));
			this.Handlers = handlers ?? Array.Empty<CatchInfo>();
			Debug.Assert(finallyState == null || finallyState.StateId > 0);
			this.FinallyState = finallyState;
			Debug.Assert(rethrowState != null);
			this.RethrowState = rethrowState;
			Debug.Assert(exitState != null);
			this.ExitState = exitState;
		}

		public CatchInfo[] Handlers {
			get;
		}

		public AsyncState FinallyState {
			get;
		}

		public AsyncState RethrowState {
			get;
		}

		public AsyncState ExitState {
			get;
		}
	}
}
