using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus.Attributes
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class OnlyComponentAttribute : Attribute, IComponentModifiedCallbackReceiver<Component>
	{
		/// <inheritdoc/>
		public void OnComponentAdded(Component attributeHolder, Component addedComponent)
		{
			DestroyComponentIfNotAttributeHolderOrTransform(attributeHolder, addedComponent);
		}

		/// <inheritdoc/>
		public void OnComponentModified(Component attributeHolder, Component modifiedComponent)
		{
			DestroyComponentIfNotAttributeHolderOrTransform(attributeHolder, modifiedComponent);
		}

		private void DestroyComponentIfNotAttributeHolderOrTransform(Component attributeHolder, Component target)
		{
			if(target == attributeHolder)
			{
				var components = attributeHolder.gameObject.GetComponents<Component>();

				#if UNITY_EDITOR
				bool userPromptedToDestroyExistingComponents = false;
				#endif
				for(int n = components.Length - 1; n >= 0; n--)
				{
					var component = components[n];
					if(component != attributeHolder && !(component is Transform))
					{
						#if UNITY_EDITOR
						if(!Application.isPlaying)
						{
							if(!userPromptedToDestroyExistingComponents)
							{
								if(UnityEditor.EditorUtility.DisplayDialog("Remove Existing Components?", "The component " + attributeHolder.GetType().Name + " is being added to a GameObject that has existing components. "+ attributeHolder.GetType().Name+" mandates that it is the only component on a GameObject. Would you like to remove the "+(components.Length - 2) + " existing components?", "Remove Existing Components", "Cancel Add Component"))
								{
									userPromptedToDestroyExistingComponents = true;
								}
								else
								{
									Object.DestroyImmediate(attributeHolder, true);
									return;
								}
							}

							UnityEditor.Undo.DestroyObjectImmediate(component);
						}
						else
						#endif
						{
							Debug.LogWarning("Removing existing component " + target.GetType().Name + " because " + attributeHolder.GetType().Name + " does not allow additional components to exist on the same GameObject.");

							Object.Destroy(component);
						}
					}
				}
			}
			else if(!(target is Transform))
			{
				Debug.LogWarning("Cannot add component "+ target.GetType().Name+" because "+attributeHolder.GetType().Name + " does not allow additional components to exist on the same GameObject.");

				#if UNITY_EDITOR
				if(!Application.isPlaying)
				{
					Object.DestroyImmediate(target, true);
				}
				else
				#endif
				{
					Object.Destroy(target);
				}
			}
		}
	}
}