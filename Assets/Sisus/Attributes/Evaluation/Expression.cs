using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using JetBrains.Annotations;

namespace Sisus.Attributes
{
	public class Expression : IShowIfEvaluator
	{
		[NotNull]
		public readonly IShowIfEvaluator left;

		[NotNull]
		public readonly IShowIfEvaluator right;

		public bool any;

		public Expression(IShowIfEvaluator setLeft, IShowIfEvaluator setRight, bool setAny)
		{
			left = setLeft;
			right = setRight;
			any = setAny;
		}

		public bool Evaluate([NotNull]Type containingClassType, [CanBeNull]object containingClassInstance, [CanBeNull]MemberInfo classMember)
		{
			#if DEV_MODE
			Debug.Log("Expression.Evaluate left and right for "+ containingClassType.Name+"/"+(classMember == null ? "null" : classMember.Name) +" with any=" + any);
			#endif

			if(left.Evaluate(containingClassType, containingClassInstance, classMember))
			{
				if(any)
				{
					return true;
				}
				return right.Evaluate(containingClassType, containingClassInstance, classMember);
			}
			if(any)
			{
				return right.Evaluate(containingClassType, containingClassInstance, classMember);
			}
			return false;
		}

		public override string ToString()
		{
			return left + " " + (any ? " | " : " & ") + " " + right;
		}
	}
}