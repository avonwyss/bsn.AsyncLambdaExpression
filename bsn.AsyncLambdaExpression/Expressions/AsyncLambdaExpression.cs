using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

using bsn.AsyncLambdaExpression.Collections;

using JetBrains.Annotations;

namespace bsn.AsyncLambdaExpression.Expressions {
	public class AsyncLambdaExpression<TDelegate>: StateMachineLambdaExpression<TDelegate> where TDelegate: Delegate {
		protected internal AsyncLambdaExpression(string name, Expression body, ReadOnlyCollection<ParameterExpression> parameters): base(name, body, parameters) { }

		public sealed override ExpressionType NodeType => ExpressionType.Extension;

		protected override Expression VisitChildren(ExpressionVisitor visitor) {
			return this.Update(visitor.Visit(this.Body), this.Parameters.Select(p => visitor.VisitAndConvert(p, null)));
		}

		public override Expression<TDelegate> BuildLambdaExpression(DebugInfoGenerator debugInfoGenerator) {
			var body = new AsyncStateMachineBuilder(this, typeof(TDelegate).GetDelegateInvokeMethod().ReturnType, debugInfoGenerator).CreateStateMachineBody();
			return Expression.Lambda<TDelegate>(body, this.Name, this.Parameters);
		}

		public AsyncLambdaExpression<TDelegate> Update([NotNull] Expression body, IEnumerable<ParameterExpression> parameters) {
			var parameterExpressions = (parameters ?? Array.Empty<ParameterExpression>()).AsReadOnlyCollection();
			return body == this.Body && (ReferenceEquals(parameterExpressions, this.Parameters) || parameterExpressions.SequenceEqual(this.Parameters)) ? this : new AsyncLambdaExpression<TDelegate>(this.Name, body, parameterExpressions);
		}

		protected override Expression Accept(ExpressionVisitor visitor) {
			return visitor is IAsyncExpressionVisitor asyncVisitor
					? asyncVisitor.VisitAsyncLambda(this)
					: this.Update(visitor.Visit(this.Body), visitor.VisitAndConvert(this.Parameters, nameof(this.Accept)));
		}
	}
}
