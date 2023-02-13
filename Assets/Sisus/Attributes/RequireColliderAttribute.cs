using System;
using UnityEngine;
using JetBrains.Annotations;

namespace Sisus.Attributes
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class RequireColliderAttribute : RequireComponentsAttribute
	{
		[CanBeNull]
		public readonly Type defaultColliderType;

		public RequireColliderAttribute() : base(typeof(Collider)) {	}

		public RequireColliderAttribute(Type defaultToColliderType) : base(typeof(Collider))
		{
			if(!typeof(Collider).IsAssignableFrom(defaultToColliderType) && !defaultToColliderType.IsInterface)
			{
				throw new ArgumentException(GetType().Name + " defaultToColliderType must be a collider type or interface!");
			}

			defaultColliderType = defaultToColliderType;
		}
	}
}