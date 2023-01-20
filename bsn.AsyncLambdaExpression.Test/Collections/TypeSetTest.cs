using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace bsn.AsyncLambdaExpression.Collections {
	public class TypeAssignableSetTest {
		private readonly ITestOutputHelper output;

		public TypeAssignableSetTest(ITestOutputHelper output) {
			this.output = output;
		}

		[Fact]
		public void Add() {
			var set = new TypeAssignableSet();
			Assert.True(set.Add(typeof(ArgumentNullException)));
			Assert.True(set.Add(typeof(ArgumentException)));
			Assert.False(set.Add(typeof(ArgumentException)));
			Assert.True(set.Add(typeof(InvalidOperationException)));
			Assert.False(set.Add(typeof(ArgumentOutOfRangeException)));
			Assert.True(set.Add(typeof(Exception)));
			Assert.False(set.Add(typeof(ApplicationException)));
		}

		[Fact]
		public void Contains() {
			var set = new TypeAssignableSet();
			Assert.True(set.Add(typeof(ArgumentException)));
			Assert.True(set.Add(typeof(InvalidOperationException)));
			Assert.True(set.Contains(typeof(ArgumentNullException)));
			Assert.True(set.Contains(typeof(ArgumentOutOfRangeException)));
			Assert.False(set.Contains(typeof(ApplicationException)));
			Assert.True(set.Add(typeof(Exception)));
			Assert.True(set.Contains(typeof(ApplicationException)));
		}
	}
}
