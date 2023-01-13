using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using bsn.AsyncLambdaExpression.Collections;

namespace bsn.AsyncLambdaExpression.Expressions {
	internal sealed class VariableScopeFinder: ExpressionVisitor {
		private readonly Dictionary<ParameterExpression, ImmutableStack<BlockExpression>> variableBlock = new(ReferenceEqualityComparer<ParameterExpression>.Default);
		private readonly HashSet<ParameterExpression> ignore = new(ReferenceEqualityComparer<ParameterExpression>.Default);
		private readonly HashSet<ParameterExpression> isread = new(ReferenceEqualityComparer<ParameterExpression>.Default);
		private readonly HashSet<BlockExpression> blockSet = new(ReferenceEqualityComparer<BlockExpression>.Default);
		private ImmutableStack<BlockExpression> blockStack = ImmutableStack<BlockExpression>.Empty;

		public VariableScopeFinder(IEnumerable<ParameterExpression> ignore) {
			if (ignore != null) {
				this.ignore.UnionWith(ignore);
			}
			IsIgnored = this.ignore.Contains;
		}

		public Func<ParameterExpression, bool> IsIgnored {
			get;
		}

		public bool IsToRemove(ParameterExpression variable) {
			return variableBlock.ContainsKey(variable) && !isread.Contains(variable);
		}

		protected override Expression VisitBinary(BinaryExpression node) {
			var unread = node.NodeType == ExpressionType.Assign && 
			             node.Left is ParameterExpression variable && 
			             !ignore.Contains(variable) && 
			             isread.Add(variable);
			try {
				return base.VisitBinary(node);
			} finally {
				if (unread) {
					isread.Remove((ParameterExpression)node.Left);
				}
			}
		}

		protected override Expression VisitParameter(ParameterExpression node) {
			if (!ignore.Contains(node)) {
				if (variableBlock.TryGetValue(node, out var stack)) {
					while (!stack.IsEmpty && !blockSet.Contains(stack.Peek())) {
						stack = stack.Pop();
					}
					variableBlock[node] = stack;
				} else {
					variableBlock.Add(node, blockStack);
				}
				isread.Add(node);
			}
			return node;
		}

		protected override CatchBlock VisitCatchBlock(CatchBlock node) {
			var ignored = node.Variable != null && ignore.Add(node.Variable);
			try {
				return node.Update(node.Variable, Visit(node.Filter), Visit(node.Body));
			} finally {
				if (ignored) {
					ignore.Remove(node.Variable);
				}
			}
		}

		protected override Expression VisitBlock(BlockExpression node) {
			blockSet.Add(node);
			blockStack = blockStack.Push(node);
			try {
				// Note: variable declarations must not be processed
				foreach (var expression in node.Expressions) {
					Visit(expression);
				}
				return node;
			} finally {
				blockSet.Remove(node);
				blockStack = blockStack.Pop();
			}
		}

		public ILookup<BlockExpression, ParameterExpression> GetBlockVariables() {
			return variableBlock
					.Where(p => isread.Contains(p.Key))
					.ToLookup(p => p.Value.PeekOrDefault(), p => p.Key);
		}
	}
}
