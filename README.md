<!-- GRAPHIC -->

# bsn.AsyncLambdaExpression

Convert plain LINQ expression trees to awaitable async state machines.

<!-- badges -->

---
## Description

Since .NET 4.0, the LINQ Expressions have become a widely used tool for runtime code generation. The
[System.Linq.Expressions](https://learn.microsoft.com/en-us/dotnet/api/system.linq.expressions) provides all
the elements needed to build fully-featured methods that are dynamically compiled at runtime.

However, while this works really great to create synchronous code, there is no built-in support for
[async/await](https://devblogs.microsoft.com/pfxteam/asyncawait-faq/). The reason for this is that async methods
cannot be represented as a plain expression or IL instructions; they in fact just start a state machine which
will interrupt execution when objects are awaited, and resume when the awaited object has completed its task.
You can get a glimpse of the internals as implemented by the C# compiler in the excellent blog post
[Dissecting the async methods in C#](https://devblogs.microsoft.com/premier-developer/dissecting-the-async-methods-in-c/).

The present library enables the transformation of normal LINQ Expression Trees to async state machines represented
as LINQ Expression Tree, which can then be comiled and awaited like normal code.

### API for awaiting expressions and async lambda creation

The class `AsyncExpressionExtensions` contains extension methods for the async/await expression trees.

```cs
Expression Await(this Expression expression, bool? continueOnCapturedContext = null)
```

Insert placeholder call with the correct return type for any awaitable object.

```cs
Expression<T> Async<T>(this LambdaExpression expression)
LambdaExpression Async(this LambdaExpression expression, Type delegateType)
```

Generic and non-generic methods for generating the state machine lambda (like `Expression.Lambda()`).

**Complete example:**

```cs
// Build a normal expression tree with "await" calls
var paraInput = Expression.Parameter(typeof(string), "input");
var exprTree = Expression.Lambda<Func<Task<string>, string>>(
		Expression.Block(
				Expression.Call(typeof(Task), nameof(Task.Delay), null, Expression.Constant(1000)).Await(false),
				paraInput.Await(false)),
		paraInput);

// Create compilable state machine async expression tree (result must be Task<?> or Task)
var asyncExprTree = exprTree.Async<Func<Task<string>, Task<string>>>();
var asyncCompiled = asyncExprTree.Compile();

// Invoke delegate as usual
var result = await asyncCompiled(Task.FromResult("test")).ConfigureAwait(false);
```

### Expressions converted to state machine states

 * Await :)
 * Binary expressions with shortcutting (AndAlso, OrElse)
 * Blocks
 * Conditional
 * Switch
 * Loop (with Continue and Break)
 * Label and Goto
 * Try..Catch..Finally (including nested and `Rethrow`)

### Known issues and limitations

 * Only `Task<>` and `Task` types can be used as return type. You cannot use `void`, `ValueTask<>`, `ValueTask`
   etc., and they would not reduce the footprint since a `TaskCompletionSource<>` is used internally.
 * The state machine requires a couple of allocations, and it has a greater memory footprint than native C# state
   machines. This is due to the constraints of LINQ Expression Trees, and also in order to optimize performance.
 * Variables which were scoped to blocks may be captured into the state machine closure if their usage spans multiple
   states. Due to this, you cannot count on variable scoping (expect all variables to be scoped for the full lambda),
   therefore you should not re-use the same variable in multiple block declarations in order to avoid unexpected behavior.
 * Nested lambda support is not tested; they currently also cannot use await (this may change in a future version).

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
- Copyright 2022 © Arsène von Wyss.
