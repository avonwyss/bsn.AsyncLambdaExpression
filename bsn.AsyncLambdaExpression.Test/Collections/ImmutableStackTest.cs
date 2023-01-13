using System;

using Xunit;
using Xunit.Abstractions;

namespace bsn.AsyncLambdaExpression.Collections {
	public class ImmutableStackTest {
		private readonly ITestOutputHelper output;

		public ImmutableStackTest(ITestOutputHelper output) {
			this.output = output;
		}

		[Fact]
		public void TestSimpleOperation() {
			var stack = ImmutableStack<int>.Empty;
			stack = stack.Push(1);
			stack = stack.Push(2);
			stack = stack.Push(3);
			Assert.Equal(3, stack.Peek());
			stack = stack.Pop();
			Assert.False(stack.IsEmpty);
			Assert.Equal(2, stack.Peek());
			stack = stack.Pop();
			Assert.False(stack.IsEmpty);
			Assert.Equal(1, stack.Peek());
			stack = stack.Pop();
			Assert.True(stack.IsEmpty);
		}

		[Fact]
		public void TestPeek() {
			var stack = ImmutableStack<int>.Empty;
			stack = stack.Push(1);
			Assert.Equal(1, stack.Peek());
			var enumerator = stack.GetEnumerator();
			stack.Peek();
			enumerator.Reset();
		}

		[Fact]
		public void TestPeekEx() {
			Assert.Throws<InvalidOperationException>(() => {
				var stack = ImmutableStack<int>.Empty;
				stack.Peek();
			});
		}

		[Fact]
		public void TestPeekEx2() {
			Assert.Throws<InvalidOperationException>(() => {
				var stack = ImmutableStack<int>.Empty;
				stack = stack.Push(1);
				stack = stack.Pop();
				stack.Peek();
			});
		}

		[Fact]
		public void TestPopEx() {
			Assert.Throws<InvalidOperationException>(() => {
				var stack = ImmutableStack<int>.Empty;
				stack.Pop();
			});
		}

		[Fact]
		public void TestPopEx2() {
			Assert.Throws<InvalidOperationException>(() => {
				var stack = ImmutableStack<int>.Empty;
				stack = stack.Push(1);
				stack = stack.Pop();
				stack.Pop();
			});
		}

		[Fact]
		public void TestEnumerator() {
			var stack = ImmutableStack<int>.Empty;
			foreach (var x in stack) {
				Assert.Fail(x.ToString());
			}
			stack = stack.Push(1);
			var i = 0;
			foreach (var x in stack) {
				Assert.Equal(0, i);
				Assert.Equal(1, x);
				i++;
			}
			i = 0;
			stack = stack.Push(2);
			stack = stack.Push(3);
			foreach (var x in stack) {
				Assert.Equal(3-i, x);
				Assert.True(i < 3);
				i++;
			}
		}
	}
}
