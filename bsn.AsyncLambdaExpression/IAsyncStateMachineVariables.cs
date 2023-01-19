using System;
using System.Linq.Expressions;

namespace bsn.AsyncLambdaExpression {
	internal interface IAsyncStateMachineVariables {
		ParameterExpression VarException {
			get;
		}

		ParameterExpression VarResumeState {
			get;
		}

		ParameterExpression VarState {
			get;
		}

		LabelTarget LblBreak {
			get;
		}

		ParameterExpression VarContinuation {
			get;
		}

		ParameterExpression GetVarAwaiter(Type awaiterType);

		Expression GetSetExceptionCall(ParameterExpression varException);
	}
}
