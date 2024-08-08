using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

using bsn.AsyncLambdaExpression.Collections;

using JetBrains.Annotations;

namespace bsn.AsyncLambdaExpression.Expressions {
	public class IteratorLambdaExpression<TDelegate>: StateMachineLambdaExpression<TDelegate> where TDelegate: Delegate {
		protected internal IteratorLambdaExpression(string name, Expression body, ReadOnlyCollection<ParameterExpression> parameters): base(name, body, parameters) { }

		public sealed override ExpressionType NodeType => ExpressionType.Extension;

		protected override Expression VisitChildren(ExpressionVisitor visitor) {
			return this.Update(visitor.Visit(this.Body), this.Parameters.Select(p => visitor.VisitAndConvert(p, null)));
		}

		public override Expression<TDelegate> BuildLambdaExpression(DebugInfoGenerator debugInfoGenerator) {
			var body = new IteratorStateMachineBuilder(this, typeof(TDelegate).GetDelegateInvokeMethod().ReturnType, debugInfoGenerator).CreateStateMachineBody();
			return Lambda<TDelegate>(body, this.Name, this.Parameters);
		}

		public IteratorLambdaExpression<TDelegate> Update([NotNull] Expression body, IEnumerable<ParameterExpression> parameters) {
			var parameterExpressions = (parameters ?? Array.Empty<ParameterExpression>()).AsReadOnlyCollection();
			return body == this.Body && parameterExpressions.SequenceEqual(this.Parameters) ? this : new IteratorLambdaExpression<TDelegate>(this.Name, body, parameterExpressions);
		}

		protected override Expression Accept(ExpressionVisitor visitor) {
			return visitor is IIteratorExpressionVisitor iteratorVisitor
					? iteratorVisitor.VisitIteratorLambda(this)
					: this.Update(visitor.Visit(this.Body), visitor.VisitAndConvert(this.Parameters, nameof(this.Accept)));
		}
	}
}
