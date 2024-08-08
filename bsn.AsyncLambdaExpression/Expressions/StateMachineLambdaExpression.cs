using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Net.PeerToPeer.Collaboration;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace bsn.AsyncLambdaExpression.Expressions {
	public abstract class StateMachineLambdaExpression: Expression {
		internal StateMachineLambdaExpression(string name, [NotNull] Expression body, ReadOnlyCollection<ParameterExpression> parameters) {
			this.Name = name;
			this.Body = body;
			this.Parameters = parameters;
		}

		/// <summary>Gets the parameters of the lambda expression.</summary>
		/// <returns>A <see cref="T:System.Collections.ObjectModel.ReadOnlyCollection`1" /> of <see cref="T:System.Linq.Expressions.ParameterExpression" /> objects that represent the parameters of the lambda expression.</returns>
		public ReadOnlyCollection<ParameterExpression> Parameters {
			get;
		}

		/// <summary>Gets the name of the lambda expression.</summary>
		/// <returns>The name of the lambda expression.</returns>
		public string Name {
			get;
		}

		/// <summary>Gets the body of the lambda expression.</summary>
		/// <returns>An <see cref="T:System.Linq.Expressions.Expression" /> that represents the body of the lambda expression.</returns>
		public Expression Body {
			get;
		}

		/// <summary>Gets the return type of the lambda expression.</summary>
		/// <returns>The <see cref="T:System.Type" /> object representing the type of the lambda expression.</returns>
		public Type ReturnType => this.Type.GetDelegateInvokeMethod().ReturnType;

		/// <summary>Produces a delegate that represents the lambda expression.</summary>
		/// <returns>A <see cref="T:System.Delegate" /> that contains the compiled version of the lambda expression.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Delegate Compile() {
			return this.CompileInternal(null);
		}

		/// <summary>Produces a delegate that represents the lambda expression.</summary>
		/// <returns>A delegate containing the compiled version of the lambda.</returns>
		/// <param name="debugInfoGenerator">Debugging information generator used by the compiler to mark sequence points and annotate local variables.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Delegate Compile(DebugInfoGenerator debugInfoGenerator) {
			return this.CompileInternal(debugInfoGenerator ?? throw new ArgumentNullException(nameof(debugInfoGenerator)));
		}

		protected abstract Delegate CompileInternal(DebugInfoGenerator debugInfoGenerator);

		/// <summary>Compiles the lambda into a method definition.</summary>
		/// <param name="method">A <see cref="T:System.Reflection.Emit.MethodBuilder" /> which will be used to hold the lambda's IL.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void CompileToMethod(MethodBuilder method) {
			this.CompileToMethodInternal(method, (DebugInfoGenerator)null);
		}

		/// <summary>Compiles the lambda into a method definition and custom debug information.</summary>
		/// <param name="method">A <see cref="T:System.Reflection.Emit.MethodBuilder" /> which will be used to hold the lambda's IL.</param>
		/// <param name="debugInfoGenerator">Debugging information generator used by the compiler to mark sequence points and annotate local variables.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void CompileToMethod(MethodBuilder method, DebugInfoGenerator debugInfoGenerator) {
			this.CompileToMethodInternal(method, debugInfoGenerator ?? throw new ArgumentNullException(nameof(debugInfoGenerator)));
		}

		protected abstract void CompileToMethodInternal([NotNull] MethodBuilder method, DebugInfoGenerator debugInfoGenerator);
	}

	public abstract class StateMachineLambdaExpression<TDelegate>: StateMachineLambdaExpression where TDelegate: Delegate {
		internal StateMachineLambdaExpression(string name, Expression body, ReadOnlyCollection<ParameterExpression> parameters): base(name, body, parameters) { }

		public sealed override Type Type => typeof(TDelegate);

		public override bool CanReduce => true;

		public override Expression Reduce() {
			return this.BuildLambdaExpression(null);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public new TDelegate Compile() {
			return (TDelegate)this.CompileInternal(null);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public new TDelegate Compile(DebugInfoGenerator debugInfoGenerator) {
			return (TDelegate)this.CompileInternal(debugInfoGenerator ?? throw new ArgumentNullException(nameof(debugInfoGenerator)));
		}

		protected sealed override Delegate CompileInternal(DebugInfoGenerator debugInfoGenerator) {
			var lambda = this.BuildLambdaExpression(debugInfoGenerator);
			return debugInfoGenerator != null
					? lambda.Compile(debugInfoGenerator)
					: lambda.Compile();
		}

		protected sealed override void CompileToMethodInternal(MethodBuilder method, DebugInfoGenerator debugInfoGenerator) {
			var lambda = this.BuildLambdaExpression(debugInfoGenerator);
			if (debugInfoGenerator != null) {
				lambda.CompileToMethod(method, debugInfoGenerator);
			} else {
				lambda.CompileToMethod(method);
			}
		}

		public abstract Expression<TDelegate> BuildLambdaExpression(DebugInfoGenerator debugInfoGenerator);
	}
}
