using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using JetBrains.Annotations;

namespace Sisus.Attributes
{
	public interface IShowIfEvaluator
	{
		bool Evaluate([NotNull]Type containingClassType, [CanBeNull]object containingClassInstance, [CanBeNull]MemberInfo classMember);
	}
}