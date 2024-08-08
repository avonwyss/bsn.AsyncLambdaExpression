using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

using bsn.AsyncLambdaExpression.Collections;

namespace bsn.AsyncLambdaExpression {
	internal abstract class StateMachineBuilderBase: IStateMachineVariables {
		// ReSharper disable InconsistentNaming
		protected static readonly ConstructorInfo ctor_InvalidOperationExpression = typeof(InvalidOperationException).GetConstructor(Type.EmptyTypes);
		// ReSharper restore InconsistentNaming

		private readonly ConcurrentDictionary<Type, ParameterExpression> varAwaiter = new();

		protected StateMachineBuilderBase(Expressions.StateMachineLambdaExpression lambda, Type breakType, DebugInfoGenerator debugInfoGenerator) {
			this.Lambda = lambda;
			this.VarState = Expression.Variable(typeof(int), "state");
			this.VarResumeState = Expression.Variable(typeof(int), "resumeState");
			this.VarException = Expression.Variable(typeof(Exception), "exception");
			this.LblBreak = Expression.Label(breakType ?? typeof(void), ":break");
			this.DebugInfoGenerator = debugInfoGenerator;
		}

		public ParameterExpression GetVarAwaiter(Type awaiterType) {
			System.Diagnostics.Debug.Assert(awaiterType.IsAwaiter());
			return this.varAwaiter.GetOrAdd(awaiterType, static t => Expression.Variable(t, "awaiter"));
		}

		public DebugInfoGenerator DebugInfoGenerator {
			get;
		}

		public ParameterExpression VarException {
			get;
		}

		public ParameterExpression VarResumeState {
			get;
		}

		public ParameterExpression VarState {
			get;
		}

		ParameterExpression IStateMachineVariables.VarCurrent => this.VarCurrent ?? throw new InvalidOperationException("Yield Return is not supported here");

		public ParameterExpression VarCurrent {
			get;
			init;
		}

		ParameterExpression IStateMachineVariables.VarContinuation => this.VarContinuation ?? throw new InvalidOperationException("Await is not supported here");

		public ParameterExpression VarContinuation {
			get;
			init;
		}

		public LabelTarget LblBreak {
			get;
		}

		protected Expressions.StateMachineLambdaExpression Lambda {
			get;
		}

		protected virtual IEnumerable<ParameterExpression> GetVars() {
			return this.varAwaiter
					.Values
					.Append(this.VarState)
					.Append(this.VarResumeState)
					.Append(this.VarContinuation)
					.Append(this.VarException);
		}

		public abstract Expression CreateStateMachineBody();

		public abstract Expression HandleException(ParameterExpression varException);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected Expression StateBodyExpressionDebug(MachineState state, Expression finallyOnly) {
			var result = this.StateBodyExpression(state, finallyOnly);
#if DEBUG
			if (this.DebugInfoGenerator != null && !string.IsNullOrEmpty(state.Name)) {
				return Expression.Block(
						Expression.Constant(state.Name),
						result);
			}
#endif
			return result;
		}

		private Expression StateBodyExpression(MachineState state, Expression finallyOnly) {
			Expression WrapForFinallyOnly(Expression expr) {
				return finallyOnly != null && !state.FinallyState
						? Expression.IfThenElse(
								finallyOnly,
								state.TryInfos.TryFirstNotNull(ti => ti.FinallyState, out var nextFinallyState)
										? Expression.Block(
												Expression.Assign(this.VarResumeState, Expression.Constant(-1)),
												Expression.Assign(this.VarState, Expression.Constant(nextFinallyState.StateId)))
										: Expression.Assign(this.VarState, Expression.Constant(-1)),
								expr)
						: expr;
			}

			var bodyExpression = state.ToExpression(this);
			if (state.TryInfos.IsEmpty || bodyExpression.IsSafeCode()) {
				return WrapForFinallyOnly(bodyExpression);
			}
			var finallyState = state.TryInfos.Peek().FinallyState;
			var rethrowState = state.TryInfos.Peek().RethrowState;
			var catchBlocks = new List<CatchBlock>();
			var catchBody = new List<Expression>();
			var varEx = default(ParameterExpression);
			var exceptionTypes = new TypeAssignableSet();
			for (var tryInfo = state.TryInfos; !tryInfo.IsEmpty; tryInfo = tryInfo.Pop()) {
				foreach (var handler in tryInfo.Peek().Handlers) {
					if (handler.Filter != null || exceptionTypes.Add(handler.Test)) {
						varEx = Expression.Variable(handler.Test, "ex");
						catchBody.Clear();
						catchBody.Add(Expression.Assign(this.VarException, varEx));
						if (handler.Variable != null) {
							catchBody.Add(Expression.Assign(handler.Variable, varEx));
						}
						if (tryInfo == state.TryInfos || finallyState == null) {
							catchBody.Add(Expression.Assign(this.VarState, Expression.Constant(handler.BodyState.StateId)));
						} else {
							catchBody.Add(Expression.Assign(this.VarResumeState, Expression.Constant(handler.BodyState.StateId)));
							catchBody.Add(Expression.Assign(this.VarState, Expression.Constant(finallyState.StateId)));
						}
						catchBlocks.Add(Expression.Catch(varEx, Expression.Block(catchBody), handler.Filter));
					}
				}
			}
			if (!exceptionTypes.Contains(typeof(Exception))) {
				varEx = Expression.Variable(typeof(Exception), "ex");
				catchBody.Clear();
				catchBody.Add(Expression.Assign(this.VarException, varEx));
				if (state.TryInfos.TryFirstNotNull(ti => ti.FinallyState, out finallyState)) {
					catchBody.Add(Expression.Assign(this.VarResumeState, Expression.Constant(rethrowState.StateId)));
					catchBody.Add(Expression.Assign(this.VarState, Expression.Constant(finallyState.StateId)));
				} else {
					catchBody.Add(Expression.Assign(this.VarState, Expression.Constant(rethrowState.StateId)));
				}
				catchBlocks.Add(Expression.Catch(varEx, Expression.Block(catchBody)));
			}
			return WrapForFinallyOnly(Expression.MakeTry(typeof(void), bodyExpression, null, null, catchBlocks));
		}
	}
}
