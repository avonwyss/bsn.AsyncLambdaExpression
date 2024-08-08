<!-- GRAPHIC -->

# bsn.AsyncLambdaExpression

Convert plain LINQ expression trees to awaitable async state machines, or iterator state machines.

<!-- badges -->

---

## Description

Since .NET 4.0, the LINQ Expressions have become a widely used tool for runtime code generation. The
[System.Linq.Expressions](https://learn.microsoft.com/en-us/dotnet/api/system.linq.expressions) provides all
the elements needed to build fully-featured methods that are dynamically compiled at runtime.

However, while this works really great to create synchronous code, there is no built-in support for
[async/await](https://devblogs.microsoft.com/pfxteam/asyncawait-faq/) or
[iterators](https://learn.microsoft.com/en-us/dotnet/csharp/iterators#enumeration-sources-with-iterator-methods).
The reason for this is that async methods and iterators cannot be represented as a plain expression or IL
instructions; they in fact just start a state machine which will interrupt execution when objects are awaited
or yielded, and resume when the awaited object has completed its task or when the iterator moves to the next item.
You can get a glimpse of the internals as implemented by the C# compiler in the excellent blog posts
[Dissecting the async methods in C#](https://devblogs.microsoft.com/premier-developer/dissecting-the-async-methods-in-c/)
and [Iterator block implementation details](https://csharpindepth.com/Articles/IteratorBlockImplementation).

The present library enables the transformation of normal LINQ Expression Trees to async or iterator state machines
represented as LINQ Expression Tree, which can then be compiled and used like normal code.

**Breaking change note:** The API for V2 has changed from extension methods to static methods, so that it is better aligned with the LINQ
`Expression` API.

### API for awaiting expressions and async lambda creation

The static class `AsyncExpression` contains methods for creating async/await-specific expression tree nodes.
The `AsyncLambdaExpression<>` will generate the state machine code when being reduced or when `BuildLambdaExpression()` is called.

---

#### Async Lambda:
```cs
AsyncLambdaExpression<TDelegate> AsyncLambda<TDelegate>(Expression body, string name, IEnumerable<ParameterExpression> parameters)
```

#### Awaiting:
```cs
AwaitExpression Await(Expression expression)
AwaitExpression AwaitConfigured(Expression expression, bool continueOnCapturedContext)
AwaitExpression AwaitConfiguredOptional(Expression expression, bool continueOnCapturedContext)
AwaitExpression AwaitIfAwaitable(Expression expression)
AwaitExpression AwaitIfAwaitableConfigured(Expression expression, bool continueOnCapturedContext)
AwaitExpression AwaitIfAwaitableConfiguredOptional(Expression expression, bool continueOnCapturedContext)
```

---

```cs
Expression<T> Async<T>(LambdaExpression expression)
LambdaExpression Async(LambdaExpression expression, Type delegateType)
```

---

**Complete example:**

```cs
// Build an expression tree for an async lambda with "await" calls
var paraInput = Expression.Parameter(typeof(string), "input");
var exprTree = AsyncExpression.AsyncLambda<Func<Task<string>, ValueTask<string>>>(
		Expression.Block(
				AsyncExpression.AwaitConfigured(
						Expression.Call(typeof(Task), nameof(Task.Delay), null, Expression.Constant(1000)), false),
				AsyncExpression.AwaitConfigured(
						paraInput, false)),
		paraInput);

// Create compilable state machine expression tree
var asyncExprTree = exprTree.BuildLambdaExpression(null);
var asyncCompiled = asyncExprTree.Compile();

// Invoke delegate as usual
var result = await asyncCompiled(Task.FromResult("test")).ConfigureAwait(false);
```

### API for yield return expressions and iterator lambda creation

The class `IteratorExpression` contains static methods for the iterator expression trees.

---

#### Iterator Lambda:
```cs
IteratorLambdaExpression<TDelegate> IteratorLambda<TDelegate>(Expression body, string name, IEnumerable<ParameterExpression> parameters)
```

#### Yielding:
```cs
YieldReturnExpression YieldReturn(Expression expression)
```
Insert placeholder call to yield the expression. Note that the `YieldReturn` will return a `void` expression.

---

**Complete example:**

```cs
// Build a normal expression tree with "yield return" calls
var lblBreak = Expression.Label("break");
var para = Expression.Parameter(typeof(int), "count");
var iteratorLambda = IteratorExpression.IteratorLambda<Func<int, IEnumerable<int>>>(
		Expression.Block(
				Expression.Loop(
						Expression.IfThenElse(
								Expression.GreaterThan(
										para,
										Expression.Constant(0)),
								Expression.PostDecrementAssign(para).YieldReturn(),
								Expression.Break(lblBreak)), lblBreak)),
		para);

// Convert to Func<int, IEnumerable<int>> expression tree
var getEnumerableLambda = iteratorLambda.BuildLambdaExpression(null);

// Compile
var getEnumerable = getEnumerableLambda.Compile();

// Use in normal code
foreach (var i in getEnumerable(10)) { ... }
```

### Expressions converted to state machine states

 * Await or Yield Return :)
 * Binary expressions with shortcutting (AndAlso, OrElse)
 * Blocks
 * Conditional
 * Switch
 * Loop (with Continue and Break)
 * Label and Goto
 * Try..Catch..Finally (including nested and `Rethrow`)

### Known issues and limitations

 * **Async:** Only `Task<>`, `Task`, `ValueTask<>` and `ValueTask` types can be used as return type. You cannot use `void`, 
   or any other type. 
 * **Iterators:** Only `IEnumerable<>` types can currently be used as return type. (`IAsyncEnumerable<>` support is planned 
   for a later version)
 * The state machine requires a few allocations, and it does have a greater memory footprint than native C# state
   machines. This is due to the constraints imposed by LINQ Expression Trees, and also in order to optimize performance.
 * Variables which were scoped to blocks may be captured into the state machine closure if their usage spans multiple
   states. Due to this, you cannot count on variable scoping (expect all variables to be scoped for the full lambda),
   therefore you should not re-use the same variable in multiple block declarations in order to avoid unexpected behavior.

<!--
---
## FAQ
- **Q**
	- A
-->

---

## Source

[https://github.com/avonwyss/bsn.AsyncLambdaExpression](https://github.com/avonwyss/bsn.AsyncLambdaExpression)

---

## Related links

- [Async lambda to Expression<Func<Task>>](https://stackoverflow.com/questions/31543468/async-lambda-to-expressionfunctask/31543991#31543991)
- [Can I generate an async method dynamically using System.Linq.Expressions?](https://stackoverflow.com/questions/24240702/can-i-generate-an-async-method-dynamically-using-system-linq-expressions)
- [Extend expression trees to cover more/all language constructs](https://github.com/dotnet/csharplang/discussions/158) 

---

## License

- **[MIT license](LICENSE.txt)**
- Copyright 2024 © Arsène von Wyss.
