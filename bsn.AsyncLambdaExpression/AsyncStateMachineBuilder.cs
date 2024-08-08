using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using bsn.AsyncLambdaExpression.Expressions;
using bsn.AsyncLambdaExpression.Tasks;

namespace bsn.AsyncLambdaExpression {
	internal class AsyncStateMachineBuilder: StateMachineBuilderBase {
		protected ICompletionSourceEmitter CompletionSourceEmitter {
			get;
			init;
		}

		protected Expression ExprCreateCompletionSource {
			get;
			init;
		}

		protected ParameterExpression VarCompletionSource {
			get;
			init;
		}

		public override Expression CreateStateMachineBody() {
			var emitter = CompletionSourceEmitterFactory.Get(this.Lambda.ReturnType);
			var varEx = Expression.Variable(typeof(Exception), "ex");
			var continuationBuilder = new ContinuationBuilder(this);
			var (finalState, finalExpr) = continuationBuilder.Process(this.Lambda.Body);
			if (finalState.StateId == 0) {
				// Nothing async, just wrap into a Task or ValueTask
				return Expression.TryCatch( // return ValueTask
								emitter.GetFromResult(this.Lambda.Body),
								Expression.Catch(varEx,
										emitter.GetFromException(varEx)))
						.Optimize();
			}
			finalState.AddExpression(
					emitter.SetResult(this.VarCompletionSource, finalExpr));
			finalState.AddExpression(
					Expression.Break(this.LblBreak));
			var variables = this.GetVars().Where(v => v != null).ToList();
			Expression stateMachine = Expression.Block(this.Lambda.ReturnType, variables,
					Expression.Assign(this.VarCompletionSource,
							this.ExprCreateCompletionSource),
					Expression.Assign(this.VarContinuation,
							Expression.Lambda<Action>(Expression.TryCatch(
									Expression.Loop(
											Expression.Switch(typeof(void), this.VarState,
													Expression.Throw(
															Expression.New(ctor_InvalidOperationExpression)),
													null,
													continuationBuilder.States.Select(state =>
															Expression.SwitchCase(
																	this.StateBodyExpressionDebug(state, null),
																	Expression.Constant(state.StateId)))), this.LblBreak),
									Expression.Catch(varEx,
											this.HandleException(varEx))))),
					Expression.Invoke(this.VarContinuation),
					emitter.GetAwaitable(this.VarCompletionSource));
			if (this.DebugInfoGenerator == null) {
				stateMachine = stateMachine.Optimize();
			}
			return stateMachine.RescopeVariables(this.Lambda.Parameters.Concat(variables));
		}

		public override Expression HandleException(ParameterExpression varException) {
			return this.CompletionSourceEmitter.SetException(this.VarCompletionSource, varException);
		}

		public AsyncStateMachineBuilder(Expressions.StateMachineLambdaExpression lambda, Type resultType, DebugInfoGenerator debugInfoGenerator): base(lambda, typeof(void), debugInfoGenerator) {
			try {
				this.CompletionSourceEmitter = CompletionSourceEmitterFactory.Get(resultType);
			} catch (Exception ex) {
				throw new ArgumentException("Only Task<>, Task, ValueTask<> and ValueTask are supported as return types", ex);
			}
			this.ExprCreateCompletionSource = this.CompletionSourceEmitter.Create();
			this.VarCompletionSource = Expression.Variable(this.ExprCreateCompletionSource.Type, "completionSource");
			this.VarContinuation = Expression.Variable(typeof(Action), "continuation");
		}

		protected override IEnumerable<ParameterExpression> GetVars() {
			return base.GetVars()
					.Append(this.VarCompletionSource);
		}
	}
}
