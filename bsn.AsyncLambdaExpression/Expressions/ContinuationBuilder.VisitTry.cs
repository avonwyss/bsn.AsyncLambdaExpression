using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using bsn.AsyncLambdaExpression.Collections;

namespace bsn.AsyncLambdaExpression.Expressions {
	internal partial class ContinuationBuilder {
		protected override Expression VisitTry(TryExpression node) {
			if (!node.RequiresStateMachine(true)) {
				return node;
			}
			if (this.rethrowState == null) {
				this.rethrowState = this.CreateState(typeof(void), ImmutableStack<TryInfo>.Empty);
				this.rethrowState.SetName("Rethrow", 0, "");
				this.rethrowState.AddExpression(
						Expression.Assign(this.vars.VarState,
								Expression.Constant(-1)));
				this.rethrowState.AddExpression(this.vars.HandleException(this.vars.VarException));
				this.rethrowState.AddExpression(
						Expression.Break(this.vars.LblBreak, Expression.Default(this.vars.LblBreak.Type)));
			}
			var tryEntryState = this.currentState;
			var tryExitState = this.CreateState(node.Type);
			tryExitState.SetName("Try", tryExitState.StateId, "Exit");
			var finallyFiber = default(Fiber);
			var finallyInfos = this.currentState.TryInfos;
			if (node.Finally != null) {
				finallyFiber = this.VisitAsFiber(node.Finally, FiberMode.Finally, finallyInfos.Push(new TryInfo(null, null, this.rethrowState, tryExitState)));
				finallyFiber.SetName("Try", tryExitState.StateId, "Finally");
				finallyFiber.ExitState.AddExpression(
						Expression.Assign(this.vars.VarState, this.vars.VarResumeState));
				finallyFiber.ExitState.AddExpression(finallyFiber.Expression);
				finallyInfos = tryEntryState.TryInfos.Push(new TryInfo(
						null,
						finallyFiber.EntryState, this.rethrowState,
						tryExitState));
			}
			var handlers = new List<CatchInfo>();
			foreach (var handler in node.Handlers) {
				var handlerFiber = this.VisitAsFiber(handler.Body, FiberMode.Standalone, finallyInfos);
				handlerFiber.SetName("Try", tryExitState.StateId, "Handler");
				handlerFiber.ContinueWith(finallyFiber.EntryState ?? tryExitState);
				if (handler.Filter.RequiresStateMachine(false)) {
					throw new InvalidOperationException("Exception filters cannot contain await");
				}
				handlers.Add(new CatchInfo(handlerFiber.EntryState, handler.Variable, handler.Test, handler.Filter));
			}
			if (node.Fault != null) {
				var faultFiber = this.VisitAsFiber(node.Fault, FiberMode.Standalone, finallyInfos);
				faultFiber.SetName("Try", tryExitState.StateId, "Fault");
				faultFiber.ContinueWith(finallyFiber.EntryState ?? tryExitState);
				handlers.Add(new CatchInfo(faultFiber.EntryState, null, typeof(Exception), null));
			}
			var bodyFiber = this.VisitAsFiber(node.Body, FiberMode.Standalone, tryEntryState.TryInfos.Push(new TryInfo(
					handlers.ToArray(),
					finallyFiber.EntryState, this.rethrowState,
					tryExitState)));
			bodyFiber.SetName("Try", tryExitState.StateId, "Body");
			tryEntryState.SetContinuation(bodyFiber.EntryState);
			if (finallyFiber.EntryState != null) {
				bodyFiber.ExitState.SetContinuation(finallyFiber.EntryState);
				bodyFiber.AssignResult(tryExitState);
			} else {
				bodyFiber.ContinueWith(tryExitState);
			}
			this.currentState = tryExitState;
			return this.currentState.ResultExpression;
		}
	}
}
