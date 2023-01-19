using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using bsn.AsyncLambdaExpression.Collections;

namespace bsn.AsyncLambdaExpression.Expressions {
	internal partial class ContinuationBuilder {
		protected override Expression VisitTry(TryExpression node) {
			if (rethrowState == null) {
				rethrowState = CreateState(typeof(void), ImmutableStack<TryInfo>.Empty);
				rethrowState.SetName("Rethrow", 0, "");
				rethrowState.AddExpression(
						Expression.Assign(
								vars.VarState,
								Expression.Constant(-1)));
				rethrowState.AddExpression(
						vars.GetSetExceptionCall(
								vars.VarException));
				rethrowState.AddExpression(
						Expression.Break(vars.LblBreak));
			}
			var tryEntryState = currentState;
			var tryExitState = CreateState(node.Type);
			tryExitState.SetName("Try", tryExitState.StateId ,"Exit");
			var finallyFiber = default(Fiber);
			var finallyInfos = currentState.TryInfos;
			if (node.Finally != null) {
				finallyFiber = VisitAsFiber(node.Finally, true, finallyInfos.Push(new TryInfo(null, null, rethrowState, tryExitState)));
				finallyFiber.SetName("Try", tryExitState.StateId, "Finally");
				finallyFiber.ExitState.AddExpression(
						Expression.Assign(
								vars.VarState,
								vars.VarResumeState));
				finallyFiber.ExitState.AddExpression(finallyFiber.Expression);
				finallyInfos = tryEntryState.TryInfos.Push(new TryInfo(
						null,
						finallyFiber.EntryState,
						rethrowState,
						tryExitState));
			}
			var handlers = new List<CatchInfo>();
			foreach (var handler in node.Handlers) {
				var handlerFiber = VisitAsFiber(handler.Body, true, finallyInfos);
				handlerFiber.SetName("Try", tryExitState.StateId, "Handler");
				handlerFiber.ContinueWith(finallyFiber.EntryState ?? tryExitState);
				if (VisitAsFiber(handler.Filter, false, finallyInfos).IsAsync) {
					throw new InvalidOperationException("Exception filters cannot contain async code, loops, labels, goto or try");
				}
				handlers.Add(new CatchInfo(handlerFiber.EntryState, handler.Variable, handler.Test, handler.Filter));
			}
			if (node.Fault != null) {
				var faultFiber = VisitAsFiber(node.Fault, true, finallyInfos);
				faultFiber.SetName("Try", tryExitState.StateId, "Fault");
				faultFiber.ContinueWith(finallyFiber.EntryState ?? tryExitState);
				handlers.Add(new CatchInfo(faultFiber.EntryState, null, typeof(Exception), null));
			}
			var bodyFiber = VisitAsFiber(node.Body, true, tryEntryState.TryInfos.Push(new TryInfo(
					handlers.ToArray(),
					finallyFiber.EntryState,
					rethrowState,
					tryExitState)));
			bodyFiber.SetName("Try", tryExitState.StateId, "Body");
			tryEntryState.SetContinuation(bodyFiber.EntryState);
			if (finallyFiber.EntryState != null) {
				bodyFiber.ExitState.SetContinuation(finallyFiber.EntryState);
				bodyFiber.AssignResult(tryExitState);
			} else {
				bodyFiber.ContinueWith(tryExitState);
			}
			currentState = tryExitState;
			return currentState.ResultExpression;
		}
	}
}
