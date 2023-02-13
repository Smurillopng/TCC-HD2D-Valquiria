using System;
using UnityEngine;
using JetBrains.Annotations;

namespace Sisus.Attributes
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class RequireCollider2DAttribute : RequireComponentsAttribute
	{
		[CanBeNull]
		public readonly Type defaultColliderType;

		public RequireCollider2DAttribute() : base(typeof(Collider2D)) {	}

		public RequireCollider2DAttribute(Type defaultToColliderType) : base(typeof(Collider2D))
		{
			if(!typeof(Collider).IsAssignableFrom(defaultToColliderType) && !defaultToColliderType.IsInterface)
			{
				throw new ArgumentException(GetType().Name + " defaultToColliderType must be a collider type or interface!");
			}

			defaultColliderType = defaultToColliderType;
		}
	}
}