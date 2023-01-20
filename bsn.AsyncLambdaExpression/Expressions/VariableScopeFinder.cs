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
			this.IsIgnored = this.ignore.Contains;
		}

		public Func<ParameterExpression, bool> IsIgnored {
			get;
		}

		public bool IsToRemove(ParameterExpression variable) {
			return this.variableBlock.ContainsKey(variable) && !this.isread.Contains(variable);
		}

		protected override Expression VisitBinary(BinaryExpression node) {
			var unread = node.NodeType == ExpressionType.Assign &&
			             node.Left is ParameterExpression variable &&
			             !this.ignore.Contains(variable) && this.isread.Add(variable);
			try {
				return base.VisitBinary(node);
			} finally {
				if (unread) {
					this.isread.Remove((ParameterExpression)node.Left);
				}
			}
		}

		protected override Expression VisitParameter(ParameterExpression node) {
			if (!this.ignore.Contains(node)) {
				if (this.variableBlock.TryGetValue(node, out var stack)) {
					while (!stack.IsEmpty && !this.blockSet.Contains(stack.Peek())) {
						stack = stack.Pop();
					}
					this.variableBlock[node] = stack;
				} else {
					this.variableBlock.Add(node, this.blockStack);
				}
				this.isread.Add(node);
			}
			return node;
		}

		protected override CatchBlock VisitCatchBlock(CatchBlock node) {
			var ignored = node.Variable != null && this.ignore.Add(node.Variable);
			try {
				return node.Update(node.Variable, this.Visit(node.Filter), this.Visit(node.Body));
			} finally {
				if (ignored) {
					this.ignore.Remove(node.Variable);
				}
			}
		}

		protected override Expression VisitBlock(BlockExpression node) {
			this.blockSet.Add(node);
			this.blockStack = this.blockStack.Push(node);
			try {
				// Note: variable declarations must not be processed
				foreach (var expression in node.Expressions) {
					this.Visit(expression);
				}
				return node;
			} finally {
				this.blockSet.Remove(node);
				this.blockStack = this.blockStack.Pop();
			}
		}

		public ILookup<BlockExpression, ParameterExpression> GetBlockVariables() {
			return this.variableBlock
					.Where(p => this.isread.Contains(p.Key))
					.ToLookup(p => p.Value.PeekOrDefault(), p => p.Key);
		}
	}
}
