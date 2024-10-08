using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

using bsn.AsyncLambdaExpression.Expressions;

using ExpressionTreeToString;

using Xunit;
using Xunit.Abstractions;

namespace bsn.AsyncLambdaExpression {
	public class IteratorExpressionExtensionTest: ExpressionExtensionTestBase {
		public IteratorExpressionExtensionTest(ITestOutputHelper output): base(output) { }

		[Fact]
		public void EnumerateAssertLazyEmpty() {
			var getEnumerable = this.GetEnumerable<int>(false,
					Throw<int>());
			using (var enumerator = getEnumerable().GetEnumerator()) {
				Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
			}
		}

		[Fact]
		public void EnumerateAssertLazyFirst() {
			var getEnumerable = this.GetEnumerable<int>(false,
					IteratorExpression.YieldReturn(
							Throw<int>()));
			using (var enumerator = getEnumerable().GetEnumerator()) {
				Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
			}
		}

		[InlineData(true)]
		[InlineData(false)]
		[Theory]
		public void EnumerateTryCatchThrow(bool debug) {
			var flag = new StrongBox<bool>(false);
			var getEnumerable = this.GetEnumerable<int>(debug,
					Expression.TryCatch(
							IteratorExpression.YieldReturn(
									Throw<int>()),
							Expression.Catch(typeof(Exception),
									Expression.Block(typeof(void),
											Expression.Assign(
													Expression.Field(
															Expression.Constant(flag),
															flag.GetType().GetStrongBoxValueField()),
													Expression.Constant(true))))));
			using (var enumerator = getEnumerable().GetEnumerator()) {
				Assert.False(flag.Value);
				Assert.False(enumerator.MoveNext());
				Assert.True(flag.Value);
			}
		}

		[InlineData(true)]
		[InlineData(false)]
		[Theory]
		public void EnumerateTryFinallyThrow(bool debug) {
			var flag = new StrongBox<bool>(false);
			var getEnumerable = this.GetEnumerable<int>(debug,
					Expression.TryFinally(
							IteratorExpression.YieldReturn(
									Throw<int>()),
							Expression.Assign(
									Expression.Field(
											Expression.Constant(flag),
											flag.GetType().GetStrongBoxValueField()),
									Expression.Constant(true))));
			using (var enumerator = getEnumerable().GetEnumerator()) {
				Assert.False(flag.Value);
				Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
			}
			Assert.True(flag.Value);
		}

		[InlineData(true)]
		[InlineData(false)]
		[Theory]
		public void EnumerateTryFinallyIncomplete(bool debug) {
			var flag = new StrongBox<bool>(false);
			var getEnumerable = this.GetEnumerable<int>(debug,
					Expression.TryFinally(
							IteratorExpression.YieldReturn(
									Expression.Constant(1)),
							Expression.Assign(
									Expression.Field(
											Expression.Constant(flag),
											flag.GetType().GetStrongBoxValueField()),
									Expression.Constant(true))),
					IteratorExpression.YieldReturn(
							Expression.Constant(2)));
			using (var enumerator = getEnumerable().GetEnumerator()) {
				Assert.False(flag.Value);
				Assert.True(enumerator.MoveNext());
				Assert.Equal(1, enumerator.Current);
			}
			Assert.True(flag.Value);
		}

		[Fact]
		public void EnumerateMultiple() {
			var getEnumerable = this.GetEnumerable<int>(false, System.Linq.Enumerable.Range(1, 4)
					.Select<int, Expression>(i =>
							IteratorExpression.YieldReturn(
									Expression.Constant(i)))
					.Append(Expression.Empty()));
			using (var enumerator1 = getEnumerable().GetEnumerator()) {
				Assert.True(enumerator1.MoveNext());
				Assert.Equal(1, enumerator1.Current);
				using (var enumerator2 = getEnumerable().GetEnumerator()) {
					Assert.True(enumerator2.MoveNext());
					Assert.Equal(1, enumerator2.Current);
					Assert.True(enumerator2.MoveNext());
					Assert.Equal(2, enumerator2.Current);
					Assert.True(enumerator2.MoveNext());
					Assert.Equal(3, enumerator2.Current);
					Assert.True(enumerator1.MoveNext());
					Assert.Equal(2, enumerator1.Current);
					Assert.True(enumerator1.MoveNext());
					Assert.Equal(3, enumerator1.Current);
					Assert.True(enumerator2.MoveNext());
					Assert.False(enumerator2.MoveNext());
				}
				Assert.True(enumerator1.MoveNext());
				Assert.Equal(4, enumerator1.Current);
				Assert.False(enumerator1.MoveNext());
			}
		}

		[InlineData(new int[0])]
		[InlineData(new[] { 1 })]
		[InlineData(new[] { 1, 2, 3, 4 })]
		[Theory]
		public void EnumerateInt(int[] items) {
			var getEnumerable = this.GetEnumerable<int>(false, items
					.Select<int, Expression>(i =>
							IteratorExpression.YieldReturn(
									Expression.Constant(i)))
					.Append(Expression.Empty()));
			using (var enumerator = getEnumerable().GetEnumerator()) {
				foreach (var item in items) {
					Assert.True(enumerator.MoveNext());
					Assert.Equal(item, enumerator.Current);
				}
				Assert.False(enumerator.MoveNext());
			}
		}

		[Fact]
		public void EnumerateLoop() {
			var lblBreak = Expression.Label("break");
			var getEnumerable = this.GetEnumerable<int, int>(false, paraCount =>
					Expression.Loop(
									Expression.IfThenElse(
											Expression.GreaterThan(
													paraCount,
													Expression.Constant(0)),
											IteratorExpression.YieldReturn(
													Expression.PostDecrementAssign(paraCount)),
											Expression.Break(lblBreak)), lblBreak)
							.Yield());
			Assert.Equal(System.Linq.Enumerable.Range(1, 10).Reverse(), getEnumerable(10));
		}

		[Fact]
		public void EnumerateLoop2() {
			var lblBreak = Expression.Label("break");
			var para = Expression.Parameter(typeof(int), "t");
			var getEnumerableLambda = IteratorExpression.IteratorLambda<Func<int, IEnumerable<int>>>(
					Expression.Block(
							Expression.Loop(
									Expression.IfThenElse(
											Expression.GreaterThan(
													para,
													Expression.Constant(0)),
											IteratorExpression.YieldReturn(
													Expression.PostDecrementAssign(para)),
											Expression.Break(lblBreak)), lblBreak)),
					para);
			var getEnumerable = getEnumerableLambda.Compile();
			Assert.Equal(System.Linq.Enumerable.Range(1, 10).Reverse(), getEnumerable(10));
		}

		private Func<IEnumerable<TResult>> GetEnumerable<TResult>(bool debug, params Expression[] expressions) {
			return this.GetEnumerable<TResult>(debug, (IEnumerable<Expression>)expressions);
		}

		private Func<IEnumerable<TResult>> GetEnumerable<TResult>(bool debug, IEnumerable<Expression> expressions) {
			var iterableLambda = IteratorExpression.IteratorLambda<Func<IEnumerable<TResult>>>(
					Expression.Block(
							expressions));
			this.Output.WriteLine("==> Original Lambda");
			this.Output.WriteLine(StubStateMachineExpressions.Process(iterableLambda).ToString(BuiltinRenderer.DebugView));
			var getEnumerableLambda = iterableLambda.BuildLambdaExpression(debug ? DebugInfoGenerator.CreatePdbGenerator() : null);
			this.Output.WriteLine("");
			this.Output.WriteLine("==> Iterator Lambda");
			this.Output.WriteLine(getEnumerableLambda.ToString(BuiltinRenderer.DebugView));
			var getEnumerable = getEnumerableLambda.Compile();
			return getEnumerable;
		}

		private Func<T, IEnumerable<TResult>> GetEnumerable<T, TResult>(bool debug, Func<ParameterExpression, IEnumerable<Expression>> expressions) {
			var para = Expression.Parameter(typeof(T), "t");
			var iterableLambda = IteratorExpression.IteratorLambda<Func<T, IEnumerable<TResult>>>(
					Expression.Block(
							expressions(para)),
					para);
			this.Output.WriteLine("==> Original Lambda");
			this.Output.WriteLine(StubStateMachineExpressions.Process(iterableLambda).ToString(BuiltinRenderer.DebugView));
			var getEnumerableLambda = iterableLambda.BuildLambdaExpression(debug ? DebugInfoGenerator.CreatePdbGenerator() : null);
			this.Output.WriteLine("");
			this.Output.WriteLine("==> Iterator Lambda");
			this.Output.WriteLine(getEnumerableLambda.ToString(BuiltinRenderer.DebugView));
			var getEnumerable = getEnumerableLambda.Compile();
			return getEnumerable;
		}
	}
}
