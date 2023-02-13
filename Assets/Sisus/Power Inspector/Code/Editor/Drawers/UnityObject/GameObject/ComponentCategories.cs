using System;
using System.Collections.Generic;
using UnityEngine;
using JetBrains.Annotations;

namespace Sisus
{
	public static class ComponentCategories
	{
		private static readonly Dictionary<Type, string> categoriesByComponent = new Dictionary<Type, string>(100);
		private static bool dictionaryBuilt;

		public static string Get([NotNull]Component component)
		{
			if(!dictionaryBuilt)
			{
				var preferences = InspectorUtility.Preferences;
				Rebuild(preferences.componentCategories, preferences.GenerateFromAddComponentMenu);
			}

			var componentType = component.GetType();

			string category;
			if(categoriesByComponent.TryGetValue(componentType, out category))
			{
				return category;
			}

			category = InspectorUtility.Preferences.defaultComponentCategory;
			categoriesByComponent.Add(componentType, category);
			return category;
		}

		public static void Rebuild(ComponentCategory[] addCategories, bool alsoAddFromComponentMenuItems)
		{
			categoriesByComponent.Clear();
			dictionaryBuilt = false;
			Build(addCategories, alsoAddFromComponentMenuItems);
		}

		private static void Build(ComponentCategory[] addCategories, bool alsoAddFromComponentMenuItems)
		{
			dictionaryBuilt = true;

			for(int n = 0, count = addCategories.Length; n < count; n++)
			{
				var addCategory = addCategories[n];
				var types = addCategory.components;
				for(int t = 0, typeCount = types.Length; t < typeCount; t++)
				{
					categoriesByComponent[types[t]] = addCategory.name;
				}
			}

			if(alsoAddFromComponentMenuItems)
			{
				BuildFromAddComponentMenuItems(AddComponentMenuItems.GetAll());
			}
		}

		private static void BuildFromAddComponentMenuItems([NotNull]AddComponentMenuItem[] menuItems)
		{
			for(int n = menuItems.Length - 1; n >= 0; n--)
			{
				var menuItem = menuItems[n];
				if(menuItem.IsGroup)
				{
					BuildFromAddComponentMenuItems(menuItem.children);
				}
				else
				{
					var type = menuItem.type;

					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(type != null && type.IsComponent());
					#endif
					
					if(!categoriesByComponent.ContainsKey(type))
					{
						var parent = menuItem.parent;
						if(parent != null)
						{
							string category = parent.label;
							if(!string.Equals(category, AddComponentMenuItems.GlobalNamespaceGroupName))
							{
								categoriesByComponent.Add(type, category);
							}
						}
					}
				}
			}
		}
	}	
}