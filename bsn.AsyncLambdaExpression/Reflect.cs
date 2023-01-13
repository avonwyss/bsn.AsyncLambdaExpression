using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace bsn.AsyncLambdaExpression {
	public static class Reflect<TType> {
		// ReSharper disable once StaticMemberInGenericType
		private static readonly ConcurrentDictionary<Type, Delegate> activators = new();

		public static TType Default;

		public static bool IsTask {
			get;
		} = typeof(TType).IsTask();

		public static Type TaskReturnType {
			get;
		} = typeof(TType).GetAsyncReturnType();

		public static bool IsValueTask {
			get;
		} = typeof(TType).IsTask();

		public static bool IsReferenceOrNullable {
			get;
		} = typeof(TType).IsValueType && Nullable.GetUnderlyingType(typeof(TType)) == null;

		public static bool HasFormulaDefault {
			get;
		} = !typeof(TType).IsValueType || Nullable.GetUnderlyingType(typeof(TType)) != null || typeof(TType) == typeof(double) || typeof(TType) == typeof(bool);

		public static TType FormulaDefault {
			get;
		} = typeof(TType) == typeof(double) ? (TType)(object)double.NaN : default(TType);

		public static PropertyInfo GetProperty<TResult>(Expression<Func<TType, TResult>> propertyAccess) {
			if ((!(propertyAccess.Body is MemberExpression expression)) || !(expression.Member is PropertyInfo)) {
				if ((propertyAccess.Body is MethodCallExpression callExpression) && (callExpression.Method.DeclaringType != null)) {
					foreach (var property in callExpression.Method.DeclaringType.GetProperties()) {
						if ((property.GetGetMethod() == callExpression.Method) || (property.GetSetMethod()==callExpression.Method)) {
							return property;
						}
					}
				}
				throw new ArgumentException("Lambda expression is not a property access");
			}
			return (PropertyInfo)expression.Member;
		}

		public static MemberInfo GetMember<TResult>(Expression<Func<TType, TResult>> memberAccess) {
			if (!(memberAccess.Body is MemberExpression expression)) {
				throw new ArgumentException("Lambda expression is not a member access");
			}
			return expression.Member;
		}

		public static FieldInfo GetField<TResult>(Expression<Func<TType, TResult>> fieldAccess) {
			if ((!(fieldAccess.Body is MemberExpression expression)) || !(expression.Member is FieldInfo) || ((FieldInfo)expression.Member).IsStatic) {
				throw new ArgumentException("Lambda expression is not an instance field access");
			}
			return (FieldInfo)expression.Member;
		}

		public static MethodInfo GetMethod(Expression<Action<TType>> methodCall) {
			if ((!(methodCall.Body is MethodCallExpression expression)) || expression.Method.IsStatic) {
				throw new ArgumentException("Lambda expression is not an instance method call");
			}
			return expression.Method;
		}

		public static Delegate GetActivator([NotNull] Type delegateType) {
			return activators.GetOrAdd(delegateType, t => {
				var method = Reflect.GetDelegateInvokeMethod(t);
				if (!method.ReturnType.IsAssignableFrom(typeof(TType))) {
					throw new ArgumentException($"The delegate {t.FullName} return type is not assignable to {typeof(TType).FullName}", nameof(delegateType));
				}
				var methodParams = method.GetParameters();
				var ctor = typeof(TType).GetConstructor(Array.ConvertAll(methodParams, p => p.ParameterType));
				if (ctor == null) {
					throw new InvalidOperationException($"No constructor found on type {typeof(TType).FullName}, expected parameters: {string.Join(", ", methodParams.Select(p => p.ParameterType.FullName))}");
				}
				var ctorParams = Array.ConvertAll(methodParams, p => Expression.Parameter(p.ParameterType, p.Name));
				// ReSharper disable once CoVariantArrayConversion
				Expression body = Expression.New(ctor, ctorParams);
				if (body.Type != method.ReturnType) {
					body = Expression.Convert(body, method.ReturnType);
				}
				return Expression.Lambda(t, body, ctorParams).Compile(false);
			});
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static TDelegate GetActivator<TDelegate>() where TDelegate: class, ICloneable, ISerializable {
			return (TDelegate)(object)GetActivator(typeof(TDelegate));
		}
	}

	public static class Reflect {
		// ReSharper disable InconsistentNaming
		private static readonly ConcurrentDictionary<Type, MethodInfo> meth_Delegate_Invoke = new();
		private static readonly ConstructorInfo ctor_TaskCompletionSourceOfObject_Flags = Reflect.GetConstructor(() => new TaskCompletionSource<object>(default(TaskCreationOptions)));
		private static readonly ConstructorInfo ctor_ValueTaskOfObject_Result = Reflect.GetConstructor(() => new ValueTask<object>(default(object)));
		private static readonly ConstructorInfo ctor_ValueTaskOfObject_Task = Reflect.GetConstructor(() => new ValueTask<object>(default(Task<object>)));
		private static readonly PropertyInfo prop_TaskCompletionSourceOfObject_Task = Reflect<TaskCompletionSource<object>>.GetProperty(s => s.Task);
		private static readonly MethodInfo meth_TaskCompletionSourceOfObject_SetResult = Reflect<TaskCompletionSource<object>>.GetMethod(s => s.SetResult(default));
		private static readonly MethodInfo meth_TaskCompletionSourceOfObject_SetException = Reflect<TaskCompletionSource<object>>.GetMethod(s => s.SetException(default(Exception)));
		// ReSharper restore InconsistentNaming
		private static readonly ConcurrentDictionary<(Type type, bool interfaces), Type[]> compatibleTypeCache = new();
		private static readonly ConcurrentDictionary<Type, (MethodInfo meth_GetAwaiter, MethodInfo meth_ConfigureAwait)> awaitableInfos = new();
		private static readonly ConcurrentDictionary<Type, (PropertyInfo prop_IsCompleted, MethodInfo meth_OnCompleted, MethodInfo meth_GetResult)> awaiterInfos = new();

		public static MethodInfo GetDelegateInvokeMethod(this Type that) {
			return meth_Delegate_Invoke.GetOrAdd(that, t => {
				if (!typeof(Delegate).IsAssignableFrom(t)) {
					return null;
				}
				var method = t.GetMethod("Invoke");
				Debug.Assert(method != null);
				return method;
			});
		}

		public static MethodInfo GetAwaitableGetAwaiterMethod(this Type that) {
			return GetAwaitableInfos(that).meth_GetAwaiter;
		}

		public static MethodInfo GetAwaitableGetConfigureAwaitMethod(this Type that) {
			return GetAwaitableInfos(that).meth_ConfigureAwait;
		}

		private static (MethodInfo meth_GetAwaiter, MethodInfo meth_ConfigureAwait) GetAwaitableInfos(Type that) {
			return awaitableInfos.GetOrAdd(that, t => {
				var methAwaiter = t.GetMethod("GetAwaiter", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
				var methConfigureAwait = t.GetMethod("ConfigureAwait", BindingFlags.Instance | BindingFlags.Public, null, new [] {typeof(bool)}, null);
				return (
						(methAwaiter != null) && methAwaiter.ReturnType.IsAwaiter() ? methAwaiter : null, 
						(methConfigureAwait != null) && methConfigureAwait.ReturnType.IsAwaitable() ? methConfigureAwait : null 
				);
			});
		}

		private static Delegate CreateAwaiter(Type type) {
			var methAwaitableGetAwaiter = type.GetAwaitableGetAwaiterMethod();
			var varAwaitable = Expression.Parameter(type, "awaitable");
			var varAwaiter = Expression.Variable(methAwaitableGetAwaiter.ReturnType, "state");
			var varTaskSource = Expression.Variable(typeof(TaskCompletionSource<object>), "taskSource");
			var varException = Expression.Variable(typeof(Exception), "ex");
			var lambda = Expression.Lambda(typeof(Func<,>).MakeGenericType(type, typeof(ValueTask<object>)),
					Expression.Block(typeof(ValueTask<object>), new[] { varAwaiter },
							Expression.Condition( // if (awaiter = awaitable.GetAwaiter()).IsCompleted
									Expression.Property(
											Expression.Assign(varAwaiter, Expression.Call(varAwaitable, methAwaitableGetAwaiter)),
											varAwaiter.Type.GetAwaiterIsCompletedProperty()),
									Expression.New(ctor_ValueTaskOfObject_Result, // return new ValueTask((object)awaiter.GetResult())
											Expression.Convert(
													Expression.Call(varAwaiter, varAwaiter.Type.GetAwaiterGetResultMethod()),
													typeof(object))),
									Expression.Block(typeof(ValueTask<object>), new[] { varTaskSource },
											Expression.Assign(varTaskSource, // taskSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously)
													Expression.New(ctor_TaskCompletionSourceOfObject_Flags,
															Expression.Constant(TaskCreationOptions.RunContinuationsAsynchronously))),
											Expression.Call(varAwaiter, varAwaiter.Type.GetAwaiterOnCompletedMethod(), // awaiter.OnCompleted(() => try { taskSource.SetResult(awaiter.GetResult()) } catch (ex) { taskSource.setException(ex) }
													Expression.Lambda<Action>(
															Expression.TryCatch(
																	Expression.Call(varTaskSource, meth_TaskCompletionSourceOfObject_SetResult,
																			Expression.Convert(
																					Expression.Call(varAwaiter, varAwaiter.Type.GetAwaiterGetResultMethod()),
																					typeof(object))),
																	Expression.Catch(varException,
																			Expression.Call(varTaskSource, meth_TaskCompletionSourceOfObject_SetException, varException))))),
											Expression.New(ctor_ValueTaskOfObject_Task, // return new ValueTask(taskSource.Task)
													Expression.Property(varTaskSource, prop_TaskCompletionSourceOfObject_Task))))),
					varAwaitable);
			return lambda.Compile();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsAwaitable(this Type that) {
			return GetAwaitableGetAwaiterMethod(that) != null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static MethodInfo GetAwaiterGetResultMethod(this Type that) {
			return AwaiterInfo(that).meth_GetResult;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static MethodInfo GetAwaiterOnCompletedMethod(this Type that) {
			return AwaiterInfo(that).meth_OnCompleted;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static PropertyInfo GetAwaiterIsCompletedProperty(this Type that) {
			return AwaiterInfo(that).prop_IsCompleted;
		}

		private static (PropertyInfo prop_IsCompleted, MethodInfo meth_OnCompleted, MethodInfo meth_GetResult) AwaiterInfo(Type that) {
			return awaiterInfos.GetOrAdd(that, t => {
				if (!typeof(INotifyCompletion).IsAssignableFrom(t)) {
					return default;
				}
				var isCompleted = t.GetProperty("IsCompleted", BindingFlags.Instance | BindingFlags.Public);
				if (isCompleted?.PropertyType != typeof(bool)) {
					return default;
				}
				var onCompleted = t.GetMethod("OnCompleted", BindingFlags.Instance|BindingFlags.Public, null, new[] {typeof(Action)}, null);
				if (onCompleted?.ReturnType != typeof(void)) {
					return default;
				}
				var getResult = t.GetMethod("GetResult", BindingFlags.Instance|BindingFlags.Public, null, Type.EmptyTypes, null);
				if (getResult == null) {
					return default;
				}
				return (isCompleted, onCompleted, getResult);
			});
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsAwaiter(this Type that) {
			return GetAwaiterGetResultMethod(that) != null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsTask(this Type that) {
			return typeof(Task).IsAssignableFrom(that);
		}

		public static Type GetAsyncReturnType(this Type that) {
			if (that.IsGenericType && (that.GetGenericTypeDefinition() == typeof(ValueTask<>))) {
				return that.GetGenericArguments()[0];
			}
			if (IsTask(that)) {
				while ((that != typeof(object)) && (that != null)) {
					if (that.IsGenericType && (that.GetGenericTypeDefinition() == typeof(Task<>))) {
						return that.GetGenericArguments()[0];
					}
					that = that.BaseType;
				}
				return null;
			}
			var asyncReturnType = that
					.GetAwaitableGetAwaiterMethod()?.ReturnType
					.GetAwaiterGetResultMethod().ReturnType;
			return asyncReturnType == typeof(void) ? null : asyncReturnType;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsValueTask(this Type that) {
			return that.IsGenericType ? that.GetGenericTypeDefinition() == typeof(ValueTask<>) : that == typeof(ValueTask);
		}

		public static MethodInfo GetStaticMethod(Expression<Action> staticMethodCall) {
			if (!(staticMethodCall.Body is MethodCallExpression expression) || !expression.Method.IsStatic) {
				throw new ArgumentException("Lambda expression is not a static method call");
			}
			return expression.Method;
		}

		public static FieldInfo GetStaticField<TResult>(Expression<Func<TResult>> fieldAccess) {
			if ((!(fieldAccess.Body is MemberExpression expression)) || !(expression.Member is FieldInfo) || !((FieldInfo)expression.Member).IsStatic) {
				throw new ArgumentException("Lambda expression is not a static field access");
			}
			return (FieldInfo)expression.Member;
		}

		public static PropertyInfo GetStaticProperty<TResult>(Expression<Func<TResult>> propertyAccess) {
			if ((!(propertyAccess.Body is MemberExpression expression)) || !(expression.Member is PropertyInfo)) {
				throw new ArgumentException("Lambda expression is not a property access");
			}
			return (PropertyInfo)expression.Member;
		}

		public static MemberInfo GetStaticMember<TResult>(Expression<Func<TResult>> memberAccess) {
			if (!(memberAccess.Body is MemberExpression expression)) {
				throw new ArgumentException("Lambda expression is not a member access");
			}
			return expression.Member;
		}

		public static ConstructorInfo GetConstructor(Expression<Action> constructorCall) {
			if (!(constructorCall.Body is NewExpression expression)) {
				throw new ArgumentException("Lambda expression is not a constructor call");
			}
			return expression.Constructor;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Expression AsExpression(this Expression<Action> that) {
			return that.Body;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Expression AsExpression<TResult>(this Expression<Func<TResult>> that) {
			return that.Body;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsObject(this Type that) {
			return that == typeof(object);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsEnumerableInterface(this Type that) {
			return that.IsInterface && that == typeof(IEnumerable) || IsGenericEnumerableInterface(that);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsGenericEnumerableInterface(this Type that) {
			return that.IsInterface && that.IsGenericType && that.GetGenericTypeDefinition() == typeof(IEnumerable<>);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsAsyncEnumerableInterface(this Type that) {
			return that.IsInterface && that.IsGenericType && that.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsAsyncEnumerable(this Type that) {
			return GetCompatibleTypes(that, true).Any(IsAsyncEnumerableInterface);
		}

		public static Type GetEnumerableItemType(this Type that) {
			if (!that.TryGetEnumerableItemType(out Type result)) {
				throw new InvalidOperationException($"The type {that.FullName} does not implement IEnumerable");
			}
			return result;
		}

		public static bool TryGetEnumerableItemType(this Type that, out Type itemType) {
			if (!typeof(IEnumerable).IsAssignableFrom(that)) {
				itemType = null;
				return false;
			}
			if (that.IsArray && that.GetArrayRank()==1) {
				itemType = that.GetElementType();
			} else {
				IEnumerable<Type> interfaces = that.GetInterfaces();
				if (that.IsInterface) {
					interfaces = interfaces.Prepend(that);
				}
				using (var e = interfaces
						.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
						.Select(t => t.GetGenericArguments()[0])
						.Where(t => !t.IsObject())
						.GetEnumerator()) {
					if (e.MoveNext()) {
						// we have a generic enumeration type
						itemType = e.Current;
						if (e.MoveNext()) {
							// ambiguous generic enumeration type, re-set to object (non-generic)
							itemType = typeof(object);
						}
					} else {
						// no generic enumeration type
						itemType = typeof(object);
					}
				}
			}
			return true;
		}

		public static bool TryGetAsyncEnumerableItemType(this Type that, out Type itemType) {
			IEnumerable<Type> interfaces = that.GetInterfaces();
			if (that.IsInterface) {
				interfaces = interfaces.Prepend(that);
			}
			var enumerableTypes = interfaces
					.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
					.Select(t => t.GetGenericArguments()[0])
					.ToList();
			if (enumerableTypes.Count > 0) {
				itemType = GetCommonType(enumerableTypes);
				return true;
			}
			itemType = null;
			return false;
		}

		public static Type GetCommonType(IEnumerable<Type> types) {
			var processedTypes = new HashSet<Type>();
			using (var enumerator = types.GetEnumerator()) {
				if (!enumerator.MoveNext()) {
					throw new InvalidOperationException("No types specified");
				}
				var firstType = enumerator.Current;
				if (!enumerator.MoveNext()) {
					// Special case - single value
					return firstType;
				}
				processedTypes.Add(firstType);
				var commonTypes = new HashSet<Type>(GetCompatibleTypes(firstType, true));
				do {
					if (processedTypes.Add(enumerator.Current)) {
						commonTypes.UnionWith(GetCompatibleTypes(enumerator.Current, true));
						if (commonTypes.Count == 0) {
							throw new InvalidOperationException($"No common types found for ${string.Join("|", processedTypes.Select(t => t.Name))}");
						}
					}
				} while (enumerator.MoveNext());
				var result = commonTypes.First();
				foreach (var commonType in commonTypes) {
					if (result.IsInterface) {
						if (commonType.IsInterface 
								? commonType.GetInterfaces().Length > result.GetInterfaces().Length
								: !commonType.IsObject()) {
							result = commonType;
						}
					} else if (GetCompatibleTypes(commonType, false).Count > GetCompatibleTypes(result, false).Count) {
						result = commonType;
					}
				}
				return result;
			}
		}

		public static IReadOnlyList<Type> GetCompatibleTypes(Type type, bool interfaces) {
			return compatibleTypeCache.GetOrAdd((type, interfaces), t => GetCompatibleTypesImpl(t.type, t.interfaces).Distinct().ToArray());
		}

		private static IEnumerable<Type> GetCompatibleTypesImpl(Type type, bool interfaces) {
			yield return type;
			if (!type.IsValueType) {
				for (var current = type.BaseType; current != null; current = current.BaseType) {
					yield return type;
				}
			}
			if (!interfaces) {
				yield break;
			}
			foreach (var implementedInterface in type.GetInterfaces()) {
				// maybe add generic support for contravariance, currently only supported for IEnumerable<*>
				if (implementedInterface.IsGenericType) {
					var typeDefinition = implementedInterface.GetGenericTypeDefinition();
					if (typeDefinition == typeof(IEnumerable<>) || typeDefinition == typeof(IAsyncEnumerable<>)) {
						var itemType = implementedInterface.GetGenericArguments()[0];
						if (!type.IsAssignableFrom(itemType)) {
							foreach (var compatibleItemType in GetCompatibleTypes(itemType, true)) {
								yield return typeDefinition.MakeGenericType(compatibleItemType);
							}
						}
					} else {
						yield return implementedInterface;
					}
				} else {
					yield return implementedInterface;
				}
			}
		}

		public static ICollection<Type> GetGenericParameters(this Type that) {
			if (!that.ContainsGenericParameters) {
				return Type.EmptyTypes;
			}
			if (that.IsGenericParameter) {
				return that.Yield();
			}
			var found = new HashSet<Type>();
			var queue = new Queue<Type>(that.GetGenericArguments());
			do {
				var type = queue.Dequeue();
				if (type.IsGenericParameter) {
					found.Add(type);
				} else if (type.ContainsGenericParameters) {
					foreach (var genericArgument in type.GetGenericArguments()) {
						queue.Enqueue(genericArgument);
					}
				}
			} while (queue.Count > 0);
			return found;
		}

		public static ICollection<T> Yield<T>(this T that) {
			return new[]{that};
		}
	}
}
