using System;
using System.Linq.Expressions;

namespace bsn.AsyncLambdaExpression {
	internal interface IStateMachineVariables {
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

		ParameterExpression VarCurrent {
			get;
		}

		ParameterExpression GetVarAwaiter(Type awaiterType);

		Expression HandleException(ParameterExpression varException);
	}
}
