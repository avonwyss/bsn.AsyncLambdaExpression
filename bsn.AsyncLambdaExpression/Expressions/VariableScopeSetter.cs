using System;
using System.Linq;
using System.Linq.Expressions;

namespace bsn.AsyncLambdaExpression.Expressions {
	internal sealed class VariableScopeSetter: ExpressionVisitor {
		private readonly ILookup<BlockExpression, ParameterExpression> blockVariables;
		private readonly Func<ParameterExpression, bool> unmanaged;
		private readonly Func<ParameterExpression, bool> toremove;

		public VariableScopeSetter(ILookup<BlockExpression, ParameterExpression> blockVariables, Func<ParameterExpression, bool> unmanaged, Func<ParameterExpression, bool> toremove) {
			this.blockVariables = blockVariables;
			this.unmanaged = unmanaged;
			this.toremove = toremove;
		}

		protected override Expression VisitBinary(BinaryExpression node) {
			if (node.NodeType == ExpressionType.Assign && node.Left is ParameterExpression variable && toremove(variable)) {
				return node.Right;
			}
			return base.VisitBinary(node);
		}

		protected override Expression VisitBlock(BlockExpression node) {
			return node.Update(
					node.Variables.Where(unmanaged).Concat(blockVariables[node]), 
					node.Expressions.Select(Visit));
		}
	}
}
