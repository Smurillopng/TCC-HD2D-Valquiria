using System;
using UnityEngine;

namespace Sisus.Attributes
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class RequireTriggerAttribute : RequireComponentsAttribute, IComponentModifiedCallbackReceiver<Collider>
	{
		/// <summary>
		/// Determines whether collider isTrigger value must be true or false.
		/// </summary>
		public bool requiredIsTriggerValue = true;

		public RequireTriggerAttribute() : base(typeof(Collider))
		{
			requiredIsTriggerValue = true;
		}

		public RequireTriggerAttribute(bool requireIsTriggerValue) : base(typeof(Collider))
		{
			requiredIsTriggerValue = requireIsTriggerValue;
		}

		public RequireTriggerAttribute(Type colliderType) : base(colliderType)
		{
			requiredIsTriggerValue = true;
		}

		public RequireTriggerAttribute(bool requireIsTriggerValue, Type colliderType) : base(colliderType)
		{
			requiredIsTriggerValue = requireIsTriggerValue;
		}

		/// <inheritdoc/>
		public void OnComponentAdded(Component attributeHolder, Collider addedComponent)
		{
			if(addedComponent.isTrigger != requiredIsTriggerValue)
			{
				addedComponent.isTrigger = requiredIsTriggerValue;
			}
		}

		/// <inheritdoc/>
		public void OnComponentModified(Component attributeHolder, Collider modifiedComponent)
		{
			if(modifiedComponent.isTrigger != requiredIsTriggerValue)
			{
				Debug.LogWarning(attributeHolder.GetType().Name + " requires that " + modifiedComponent.GetType().Name + ".isTrigger is " + requiredIsTriggerValue + ".");

				modifiedComponent.isTrigger = requiredIsTriggerValue;
			}
		}
	}
}