using System;
using UnityEngine;

namespace Sisus.Attributes
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class RequireTrigger2DAttribute : RequireComponentsAttribute, IComponentModifiedCallbackReceiver<Collider2D>
	{
		/// <summary>
		/// Determines whether collider isTrigger value must be true or false.
		/// </summary>
		public bool requiredIsTriggerValue = true;

		public RequireTrigger2DAttribute() : base(typeof(Collider2D))
		{
			requiredIsTriggerValue = true;
		}

		public RequireTrigger2DAttribute(bool requireIsTriggerValue) : base(typeof(Collider2D))
		{
			requiredIsTriggerValue = requireIsTriggerValue;
		}

		public RequireTrigger2DAttribute(Type colliderType) : base(colliderType)
		{
			requiredIsTriggerValue = true;
		}

		public RequireTrigger2DAttribute(bool requireIsTriggerValue, Type colliderType) : base(colliderType)
		{
			requiredIsTriggerValue = requireIsTriggerValue;
		}

		/// <inheritdoc/>
		public void OnComponentAdded(Component attributeHolder, Collider2D addedComponent)
		{
			addedComponent.isTrigger = requiredIsTriggerValue;
		}

		/// <inheritdoc/>
		public void OnComponentModified(Component attributeHolder, Collider2D modifiedComponent)
		{
			if(modifiedComponent.isTrigger != requiredIsTriggerValue)
			{
				Debug.LogWarning(attributeHolder.GetType().Name+ " requires that "+ modifiedComponent.GetType().Name+".isTrigger is "+ requiredIsTriggerValue+".");

				modifiedComponent.isTrigger = requiredIsTriggerValue;
			}
		}
	}
}