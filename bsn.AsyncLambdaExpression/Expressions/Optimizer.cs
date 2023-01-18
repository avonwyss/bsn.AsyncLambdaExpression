using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace bsn.AsyncLambdaExpression.Expressions {
	internal class Optimizer: ExpressionVisitor {
		private static readonly HashSet<ExpressionType> assignmentTypes = new() {
				ExpressionType.Assign,
				ExpressionType.AddAssign,
				ExpressionType.AddAssignChecked,
				ExpressionType.AndAssign,
				ExpressionType.DivideAssign,
				ExpressionType.ExclusiveOrAssign,
				ExpressionType.LeftShiftAssign,
				ExpressionType.ModuloAssign,
				ExpressionType.MultiplyAssign,
				ExpressionType.MultiplyAssignChecked,
				ExpressionType.OrAssign,
				ExpressionType.AddAssign,
				ExpressionType.PowerAssign,
				ExpressionType.PreDecrementAssign,
				ExpressionType.PreIncrementAssign,
				ExpressionType.RightShiftAssign,
				ExpressionType.SubtractAssign,
				ExpressionType.SubtractAssignChecked
		}; // this list does not contain PostXxxAssign since these operators return a different value as result than the assigned value

		protected override Expression VisitBlock(BlockExpression node) {
			var expressions = new List<Expression>(node.Expressions.Count * 2);
			var variables = node.Variables.ToList();
			var last = default(Expression);
			var queue = new Queue<Expression>();
			foreach (var exprs in node.Expressions.Select(Visit)) {
				queue.Enqueue(exprs);
				do {
					var expr = queue.Dequeue();
					last = default;
					if (expr is DefaultExpression or ConstantExpression or ParameterExpression) {
						// Don't add a pure expression, but remember it in case it was the last expression
						last = expr;
					} else if (expr is BlockExpression nestedBlock) {
						// Flatten nested block by adding items to the queue
						foreach (var nested in nestedBlock.Expressions) {
							queue.Enqueue(nested);
						}
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
				} while (queue.Count > 0);
			}
			if (last != null &&
			    (expressions.Count == 0 || !(
					    expressions[expressions.Count-1] is BinaryExpression binary &&
					    assignmentTypes.Contains(binary.NodeType) &&
					    binary.Left == last)
			    )) {
				// Keep "last" expression if it is an expression which was skipped over, and the previous one was not an assignment to "last"
				expressions.Add(last);
			}
			while (expressions.Count >= 2 && 
			       expressions[expressions.Count-2] is BinaryExpression {NodeType: ExpressionType.Assign, Left: ParameterExpression para1 } assign1 &&
			       expressions[expressions.Count-1] is BinaryExpression {NodeType: ExpressionType.Assign, Conversion: null, Left: ParameterExpression para2 } assign2 &&
			       assign2.Right == para1) {
				expressions[expressions.Count-2] = assign1.Update(para2, assign1.Conversion, assign1.Right);
				expressions.RemoveAt(expressions.Count-1);
			}
			return expressions.Count == 0
					? Expression.Default(node.Type)
					: expressions.Count == 1 && node.Type == expressions[0].Type && variables.Count == 0
							? expressions[0]
							: node.Update(variables, expressions);
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
