using System;
using UnityEngine;
using JetBrains.Annotations;

namespace Sisus.Attributes
{
	/// <summary>
	/// 
	/// 
	/// This functions like the built-in RequireComponentAttribute, but supports using base classes and requiring one of multiple components.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class RequireComponentsAttribute : Attribute, IRequireComponents
	{
		/// <summary>
		/// Array of required components.
		/// </summary>
		[NotNull]
		public readonly Type[] requiredComponents;

		/// <summary>
		/// If true then all components are required, if false then just one of the components is required.
		/// </summary>
		public readonly bool allRequired = true;

		/// <inheritdoc/>
		public Type[] RequiredComponents
		{
			get
			{
				return requiredComponents;
			}
		}

		/// <inheritdoc/>
		public bool AllRequired
		{
			get
			{
				return allRequired;
			}
		}

		public RequireComponentsAttribute([NotNull]Type requiredComponent)
		{
			if(!typeof(Component).IsAssignableFrom(requiredComponent) && !requiredComponent.IsInterface)
			{
				throw new ArgumentException(GetType().Name + " requiredComponent must be a Component type or interface!");
			}

			requiredComponents = new Type[] { requiredComponent };
			allRequired = true;
		}

		public RequireComponentsAttribute([NotNull]params Type[] requireComponents)
		{
			int count = requireComponents.Length;
			if(count == 0)
			{
				throw new ArgumentException(GetType().Name + " requireComponents can not be an empty array!");
			}

			for(int n = requireComponents.Length - 1; n >= 0; n--)
			{
				var requiredComponent = requireComponents[n];
				if(!typeof(Component).IsAssignableFrom(requiredComponent) && !requiredComponent.IsInterface)
				{
					throw new ArgumentException(GetType().Name + " requiredComponent must be a Component type or interface!");
				}
			}

			requiredComponents = requireComponents;
			allRequired = true;
		}

		protected RequireComponentsAttribute(bool requireAll, [NotNull]params Type[] requireComponents)
		{
			int count = requireComponents.Length;
			if(count == 0)
			{
				throw new ArgumentException(GetType().Name + " requireComponents can not be an empty array!");
			}

			for(int n = requireComponents.Length - 1; n >= 0; n--)
			{
				var requiredComponent = requireComponents[n];
				if(!typeof(Component).IsAssignableFrom(requiredComponent) && !requiredComponent.IsInterface)
				{
					throw new ArgumentException(GetType().Name + " requiredComponent must be a Component type or interface!");
				}
			}

			requiredComponents = requireComponents;
			allRequired = count == 1 || requireAll;
		}
	}
}