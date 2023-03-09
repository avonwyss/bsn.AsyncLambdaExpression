using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace bsn.AsyncLambdaExpression.Expressions {
	internal sealed class SafeCodeChecker: ExpressionVisitor {
		private static readonly HashSet<ExpressionType> SafeNodes = new() {
				ExpressionType.Assign,
				ExpressionType.AddAssign,
				ExpressionType.Add,
				ExpressionType.And,
				ExpressionType.AndAssign,
				ExpressionType.AndAlso,
				ExpressionType.Block,
				ExpressionType.Coalesce,
				ExpressionType.Conditional,
				ExpressionType.DebugInfo,
				ExpressionType.Constant,
				ExpressionType.Default,
				ExpressionType.Decrement,
				ExpressionType.Equal,
				ExpressionType.ExclusiveOr,
				ExpressionType.ExclusiveOrAssign,
				ExpressionType.GreaterThan,
				ExpressionType.GreaterThanOrEqual,
				ExpressionType.Increment,
				ExpressionType.IsTrue,
				ExpressionType.IsFalse,
				ExpressionType.TypeAs,
				ExpressionType.Label,
				ExpressionType.Goto,
				ExpressionType.LeftShift,
				ExpressionType.LeftShiftAssign,
				ExpressionType.LessThan,
				ExpressionType.LessThanOrEqual,
				ExpressionType.Loop,
				ExpressionType.Multiply,
				ExpressionType.MultiplyAssign,
				ExpressionType.Negate,
				ExpressionType.Not,
				ExpressionType.NotEqual,
				ExpressionType.OnesComplement,
				ExpressionType.Or,
				ExpressionType.OrElse,
				ExpressionType.OrAssign,
				ExpressionType.Parameter,
				ExpressionType.PostDecrementAssign,
				ExpressionType.PostIncrementAssign,
				ExpressionType.Power,
				ExpressionType.PreDecrementAssign,
				ExpressionType.PowerAssign,
				ExpressionType.PreIncrementAssign,
				ExpressionType.Quote,
				ExpressionType.RightShift,
				ExpressionType.RightShiftAssign,
				ExpressionType.Subtract,
				ExpressionType.SubtractAssign,
				ExpressionType.Switch,
				ExpressionType.TypeEqual,
				ExpressionType.TypeIs,
				ExpressionType.Try,
				ExpressionType.UnaryPlus,
				ExpressionType.Unbox
		};

		protected override Expression VisitBinary(BinaryExpression node) {
			if (node.Method != null) {
				this.ContainsUnsafeCode = true;
				return node;
			}
			if (node.Left is MemberExpression {
					    Expression: ParameterExpression para,
					    Member: FieldInfo {
							    Name: nameof(StrongBox<object>.Value)
					    } field
			    }
			    && para.Type.IsStrongBox()
			    && field.DeclaringType.IsStrongBox()) {
				return node.Update(node.Left, node.Conversion, this.Visit(node.Right));
			}
			return base.VisitBinary(node);
		}

		protected override Expression VisitUnary(UnaryExpression node) {
			if (node.Method != null) {
				this.ContainsUnsafeCode = true;
				return node;
			}
			return base.VisitUnary(node);
		}

		protected override Expression VisitSwitch(SwitchExpression node) {
			if (node.Comparison != null) {
				this.ContainsUnsafeCode = true;
				return node;
			}
			return base.VisitSwitch(node);
		}

		public override Expression Visit(Expression node) {
			if (node == null) {
				return null;
			}
			if (this.ContainsUnsafeCode) {
				return node;
			}
			if (!SafeNodes.Contains(node.NodeType)) {
				this.ContainsUnsafeCode = true;
				return node;
			}
			return base.Visit(node);
		}

		public bool ContainsUnsafeCode {
			get;
			private set;
		}
	}
}
