using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace bsn.AsyncLambdaExpression {
	internal class OptimizeVisitor: ExpressionVisitor {
		protected override Expression VisitBlock(BlockExpression node) {
			var expressions = new List<Expression>(node.Expressions.Count * 2);
			var variables = node.Variables.ToList();
			var last = default(Expression);
			foreach (var expr in node.Expressions.Select(Visit)) {
				last = default;
				if (expr is DefaultExpression or ConstantExpression or ParameterExpression) {
					// Don't add a pure expression, but remember it in case it was the last expression
					last = expr;
				} else if (expr is BlockExpression nestedBlock) {
					// Merge nested block into current block
					expressions.AddRange(nestedBlock.Expressions);
					variables.AddRange(nestedBlock.Variables);
				} else if (expr is GotoExpression exprGoto) {
					// Stop after the goto, but make sure the result type matches
					expressions.Add(node.Type == exprGoto.Type
							? exprGoto
							: Expression.MakeGoto(exprGoto.Kind, exprGoto.Target, exprGoto.Value, node.Type));
					break;
				} else if (expr is UnaryExpression { NodeType: ExpressionType.Throw } exprThrow) {
					// Stop after the throw, but make sure the result type matches
					expressions.Add(node.Type == exprThrow.Type
							? expr
							: Expression.Throw(exprThrow.Operand, node.Type));
					break;
				} else {
					expressions.Add(expr);
				}
			}
			if (last != null) {
				// Restore last expression if it is an expression which was skipped over
				expressions.Add(last);
			}
			// TODO: Check that variables are used in subtree
			return expressions.Count == 0
					? Expression.Default(node.Type)
					: expressions.Count == 1 && node.Type == expressions[0].Type && variables.Count == 0
							? expressions[0]
							: Expression.Block(node.Type, variables, expressions);
		}

		public override Expression Visit(Expression node) {
			var result = base.Visit(node);
			while (result is { CanReduce: true }) {
				result = result.Reduce();
			}
			return result;
		}
	}
}
