using System;
using System.Reflection;
using JetBrains.Annotations;

namespace Sisus.Attributes
{
	public class Negation : IShowIfEvaluator
	{
		[NotNull]
		public readonly IShowIfEvaluator expressionToNegate;

		public Negation(IShowIfEvaluator setExpressionToNegate)
		{
			expressionToNegate = setExpressionToNegate;
		}

		public bool Evaluate([NotNull]Type containingClassType, [CanBeNull]object containingClassInstance, [CanBeNull]MemberInfo classMember)
		{
			return !expressionToNegate.Evaluate(containingClassType, containingClassInstance, classMember);
		}

		public override string ToString()
		{
			return "!(" + expressionToNegate + ")";
		}
	}
}