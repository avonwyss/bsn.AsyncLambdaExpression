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
			if (node.NodeType == ExpressionType.Assign && node.Left is ParameterExpression variable && this.toremove(variable)) {
				return node.Right;
			}
			return base.VisitBinary(node);
		}

		protected override Expression VisitBlock(BlockExpression node) {
			var block = node.Update(
					node.Variables.Where(this.unmanaged).Concat(this.blockVariables[node]),
					node.Expressions.Select(this.Visit));
			if (block.Variables.Count > 0) {
				return block;
			}
			// Eliminate now-redundant blocks
			return block.Expressions.Count switch {
					0 => Expression.Default(node.Type),
					1 => block.Expressions[0],
					_ => block
			};
		}
	}
}
