using System;
using System.Diagnostics;
using System.Linq;

namespace bsn.AsyncLambdaExpression {
	internal struct TryInfo {
		public TryInfo(CatchInfo[] handlers, MachineState finallyState, MachineState rethrowState, MachineState exitState) {
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

		public MachineState FinallyState {
			get;
		}

		public MachineState RethrowState {
			get;
		}

		public MachineState ExitState {
			get;
		}
	}
}
