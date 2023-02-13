using System.Reflection;
using UnityEngine;
using JetBrains.Annotations;
using Sisus.Attributes;

namespace Sisus
{
	public static class ComponentModifiedCallbackUtility
	{
		/// <summary>
		/// This should be called whenever a new component is added to a GameObject through Power Inspector.
		/// </summary>
		/// <param name="addedComponent"> The component that was just added to a GameObject. </param>
		public static void OnComponentAdded([NotNull]Component addedComponent)
		{
			HandleCallbacks(addedComponent, true);
		}

		/// <summary>
		/// This should be called whenever a component is modified through Power Inspector.
		/// </summary>
		/// <param name="modifiedComponent"> The component that was just modified. </param>
		public static void OnComponentModified([NotNull]Component modifiedComponent)
		{
			HandleCallbacks(modifiedComponent, false);
		}

		private static void HandleCallbacks([NotNull]Component addedOrModifiedComponent, bool isAddComponentEvent)
		{
			#if DEV_MODE
			Debug.Log("ComponentModifiedCallbackUtility." + (isAddComponentEvent ? "OnComponentAdded" : "OnComponentModified") + "(" + addedOrModifiedComponent .GetType().Name + ")");
			#endif

			var gameObject = addedOrModifiedComponent.gameObject;
			var components = gameObject.GetComponents<Component>();
			for(int c = components.Length - 1; c >= 0; c--)
			{
				var checkComponentAttributes = components[c];
				if(checkComponentAttributes == null)
				{
					continue;
				}

				var attributes = checkComponentAttributes.GetType().GetCustomAttributes(true);
				for(int a = attributes.Length - 1; a >= 0; a--)
				{
					var attribute = attributes[a];
					var attributeType = attribute.GetType();
					var interfaces = attributeType.GetInterfaces();
					for(int i = interfaces.Length - 1; i >= 0; i--)
					{
						var implementedInterface = interfaces[i];
						if(implementedInterface.IsGenericType)
						{
							#if DEV_MODE
							Debug.Log("Generic interface ("+ StringUtils.ToString(implementedInterface)+") found on attribute "+attributeType.Name+" on component "+ checkComponentAttributes.GetType().Name);
							#endif

							var typeDefinition = implementedInterface.GetGenericTypeDefinition();
							if(typeDefinition == typeof(IComponentModifiedCallbackReceiver<>))
							{
								#if DEV_MODE
								Debug.Log("IComponentModifiedCallbackReceiver found on component "+ checkComponentAttributes.GetType().Name);
								#endif

								var interfaceGenericType = implementedInterface.GetGenericArguments()[0];
								var method = attributeType.GetMethod(isAddComponentEvent ? "OnComponentAdded" : "OnComponentModified", BindingFlags.Instance | BindingFlags.Public);

								if(checkComponentAttributes != addedOrModifiedComponent)
								{
									#if DEV_MODE
									Debug.Log("Testing if "+ interfaceGenericType.Name+" is assignable from " + addedOrModifiedComponent.GetType().Name+"...");
									#endif
								
									if(interfaceGenericType.IsAssignableFrom(addedOrModifiedComponent.GetType()))
									{
										#if DEV_MODE
										Debug.Log("Calling "+method.Name+" on "+attributeType.Name+" on component "+ checkComponentAttributes.GetType().Name + " with "+(isAddComponentEvent ? "added" : "modified") + " component: " + addedOrModifiedComponent.GetType().Name);
										#endif

										method.InvokeWithParameters(attribute, checkComponentAttributes, addedOrModifiedComponent);
									}
								}
								else if(isAddComponentEvent)
								{
									for(int c2 = components.Length - 1; c2 >= 0; c2--)
									{
										var preExistingComponent = components[c2];
										if(interfaceGenericType.IsAssignableFrom(preExistingComponent.GetType()))
										{
											#if DEV_MODE
											Debug.Log("Calling OnComponentAdded on "+attributeType.Name+" on component "+ checkComponentAttributes.GetType().Name + " with pre-existing component: " + preExistingComponent.GetType().Name);
											#endif

											method.InvokeWithParameters(attribute, checkComponentAttributes, preExistingComponent);
										}
									}
								}
							}
						}
					}
				}				
			}
		}
	}
}