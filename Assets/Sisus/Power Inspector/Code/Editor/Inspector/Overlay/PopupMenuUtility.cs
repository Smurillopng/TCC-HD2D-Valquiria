//#define WARN_ABOUT_CONFLICTING_TYPE_NAMES
//#define DEBUG_ADD_RANGE_TIME

using System.Collections.Generic;

using System;
#if PI_ASSERTATIONS && WARN_ABOUT_CONFLICTING_TYPE_NAMES
using System.Linq;
#endif
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;
using Sisus.Attributes;
using System.Linq;

namespace Sisus
{
	public static class PopupMenuUtility
	{
		private static readonly Dictionary<Type, string[]> typesLabelsForTypesVisibleFromContext = new Dictionary<Type, string[]>();
		private static readonly List<Component> getComponents = new List<Component>();
		private static readonly Dictionary<Type, string> tooltips = new Dictionary<Type, string>();
		private static readonly HashSet<string> MenuLabels = new HashSet<string>(); //new HashSet<string>(9000);

		/// <summary>
		/// Get list of all visible types and hidden types accessible from given context
		/// </summary>
		/// <param name="rootItems"> [in,out] The root items in the menu. </param>
		/// <param name="groupsByLabel">[in,out] Any built groups will be added to the list with their full label as key. </param>
		/// <param name="itemsByLabel">[in,out] Built item will be added to the list with ther full label as key. </param>
		/// <param name="typeContext">
		/// Context for the type. This may be null. </param>
		/// <param name="addNull"> True to add null. </param>
		public static void BuildTypePopupMenuItemsForContext(ref List<PopupMenuItem> rootItems, ref Dictionary<string, PopupMenuItem> groupsByLabel, ref Dictionary<string, PopupMenuItem> itemsByLabel, [CanBeNull]Type typeContext, bool addNull)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("BuildTypePopupMenuItemsForContext");
			#endif

			var menuDrawer = PopupMenu.instance;
			if(menuDrawer != null)
			{
				if(menuDrawer.builtFromTypeContext == typeContext)
				{
					rootItems = menuDrawer.rootItems;
					groupsByLabel = menuDrawer.groupsByLabel;
					itemsByLabel = menuDrawer.itemsByLabel;
					return;
				}
				PopupMenu.instance.DisposeItems();
			}

			string[] typeLabels;
			var types = GenerateTypesVisibleFromContext(typeContext, out typeLabels);

			int i = 0;
			foreach(var type in types)
			{
				var label = typeLabels[i];

				// Sometimes multiple assemblies can contain types with exact same full name. Skip these duplicates.
				if(MenuLabels.Add(label))
				{
					BuildPopupMenuItemForTypeWithLabel(ref rootItems, ref groupsByLabel, ref itemsByLabel, type, label);
				}
				i++;
			}

			rootItems.Sort();
			for(int n = rootItems.Count - 1; n >= 0; n--)
			{
				rootItems[n].Sort();
			}
			
			if(addNull && MenuLabels.Add("None"))
			{
				var nullItem = PopupMenuItem.Item(null as Type, "None", "A null reference; one that does not refer to any object.", null);
				nullItem.Preview = null;
				rootItems.Insert(0, nullItem);
				itemsByLabel.Add(nullItem.label, nullItem);
			}
			
			MenuLabels.Clear();

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
		}
		
		public static IEnumerable<Type> GenerateTypesVisibleFromContext([CanBeNull]Type typeContext, out string[] typeLabels)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("GenerateTypesVisibleFromContext");
			#endif

			IEnumerable<Type> types;
			if(typeContext != null)
			{
				types = typeContext.GetTypesAccessibleFromContext();
				typeLabels = TypeExtensions.GetPopupMenuLabels(types);

				if(!typesLabelsForTypesVisibleFromContext.TryGetValue(typeContext, out typeLabels))
				{
					typeLabels = TypeExtensions.GetPopupMenuLabels(types);
					typesLabelsForTypesVisibleFromContext.Add(typeContext, typeLabels);
				}

				#if DEV_MODE
				Debug.Log(StringUtils.ToColorizedString("GenerateTypesVisibleFromContext(", typeContext, "): ", typeLabels.Length, "\n", StringUtils.ToString(typeLabels, "\n")));
				#endif
			}
			else
			{
				types = TypeExtensions.AllVisibleTypes as Type[];
				if(types == null)
                {
					types = TypeExtensions.AllVisibleTypes.ToArray();
				}
				typeLabels = TypeExtensions.MenuLabelsForAllVisibleTypes;

				#if DEV_MODE
				Debug.Log(StringUtils.ToColorizedString("GenerateTypesVisibleFromContext(", null, "): ", typeLabels.Length, "\n", StringUtils.ToString(typeLabels, "\n")));
				#endif
			}

			#if DEV_MODE && PI_ASSERTATIONS
			int count = typeLabels.Length;
			if(count != typeLabels.Length)
			{
				Debug.LogError("types.Length ("+count+") != typeLabels.Length ("+typeLabels.Length+")");
			}
			else
			{
				var typesUnique = new HashSet<Type>();
				#if WARN_ABOUT_CONFLICTING_TYPE_NAMES
				HashSet<string> labelsUnique = new HashSet<string>();
				bool labelsFailedTest = false;
				#endif
				//for(int n = 0; n < count; n++)
				foreach(var type in types)
				{
					if(!typesUnique.Add(type))
					{
						Debug.LogError("types contained multiple instances of "+StringUtils.ToString(type));
					}

					#if WARN_ABOUT_CONFLICTING_TYPE_NAMES
					var label = typeLabels[n];
					if(!labelsUnique.Add(label))
					{
						labelsFailedTest = true;
					}
					#endif
				}

				#if WARN_ABOUT_CONFLICTING_TYPE_NAMES
				if(labelsFailedTest)
				{
					var grouped = types.GroupBy(type => TypeExtensions.GetPopupMenuLabel(type));
					var nonUnique = grouped.Where(group => group.Count() > 1).Select(grouping => grouping.GetEnumerator());
					Debug.LogWarning("Types With Non-Unique Full Names:\n" + StringUtils.ToString(nonUnique, "\n"));
				}
				#endif
			}
			#endif

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
			return types;
		}

		/// <summary>
		/// Get list of all possible values of enum
		/// </summary>
		/// <param name="results">
		/// [in,out] The popupmenu item will be added to the results list, either directly in the root if it has no group parents,
		/// or nested inside a group in the list - which will also get created if needed.
		/// </param>
		/// <param name="groupsByLabel">[in,out] Any built groups will be added to the list with their full label as key. </param>
		/// <param name="itemsByLabel">[in,out] Built item will be added to the list with ther full label as key. </param>
		/// <param name="enumType"> Type of the enum. </param>
		public static void BuildPopupMenuItemsForEnumType(ref List<PopupMenuItem> results, ref Dictionary<string, PopupMenuItem> groupsByLabel, ref Dictionary<string, PopupMenuItem> itemsByLabel, [NotNull]Type enumType)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("BuildPopupMenuItemsForEnumType");
			#endif

			var popupDrawer = PopupMenu.instance;
			if(popupDrawer != null)
			{
				if(popupDrawer.builtFromTypeContext == enumType)
				{
					results = popupDrawer.rootItems;
					groupsByLabel = popupDrawer.groupsByLabel;
					itemsByLabel = popupDrawer.itemsByLabel;
					return;
				}
				PopupMenu.instance.DisposeItems();
			}

			var underlyingType = Enum.GetUnderlyingType(enumType);
			var enumValues = Enum.GetValues(enumType);
			var enumNames = Enum.GetNames(enumType);

			int count = enumValues.Length;
			for(int n = 0; n < count; n++)
			{
				var enumValue = enumValues.GetValue(n);

				string name = enumNames[n];

				#if UNITY_2019_2_OR_NEWER
				var memberInfo = enumType.GetMember(name).FirstOrDefault();
				InspectorNameAttribute inspectorName;
				if(Attribute<InspectorNameAttribute>.TryGet(memberInfo, false, out inspectorName))
                {
					name = inspectorName.displayName;
				}
				#endif

				BuildPopupMenuItemWithLabel(ref results, ref groupsByLabel, ref itemsByLabel, enumValue, null, name, StringUtils.ToString(Convert.ChangeType(enumValue, underlyingType)), null);
			}

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
		}
		
		public static PopupMenuItem FindMenuItemByLabel(List<PopupMenuItem> searchItems, string label)
		{
			for(int n = searchItems.Count - 1; n >= 0; n--)
			{
				var item = searchItems[n];

				if(string.Equals(item.label, label))
				{
					return item;
				}

				if(item.IsGroup)
				{
					var tryFind = FindMenuItemByLabel(item.children, label);
					if(tryFind != null)
					{
						return tryFind;
					}
				}
			}
			return null;
		}

		public static PopupMenuItem FindMenuItemByType(List<PopupMenuItem> searchItems, Type type)
		{
			for(int n = searchItems.Count - 1; n >= 0; n--)
			{
				var item = searchItems[n];
				
				if(item.type == type)
				{
					return item;
				}

				if(item.IsGroup)
				{
					var tryFind = FindMenuItemByType(item.children, type);
					if(tryFind != null)
					{
						return tryFind;
					}
				}
			}
			return null;
		}

		public static PopupMenuItem FindMenuItemByIdentifyingObject(List<PopupMenuItem> searchItems, object identifyingObject)
		{
			for(int n = searchItems.Count - 1; n >= 0; n--)
			{
				var item = searchItems[n];
				
				if(item.IdentifyingObject == identifyingObject)
				{
					return item;
				}

				if(item.IsGroup)
				{
					var tryFind = FindMenuItemByIdentifyingObject(item.children, identifyingObject);
					if(tryFind != null)
					{
						return tryFind;
					}
				}
			}
			return null;
		}
		
		public static void BuildPopupMenuItemForType([NotNull]List<PopupMenuItem> results, Dictionary<string, PopupMenuItem> groupsByLabel, Dictionary<string, PopupMenuItem> itemsByLabel, [NotNull]Type type, string globalNamespaceTypePrefix = "")
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("BuildPopupMenuItemForType");
			#endif
			string path = TypeExtensions.GetPopupMenuLabel(type, globalNamespaceTypePrefix);
			BuildPopupMenuItemForTypeWithLabel(ref results, ref groupsByLabel, ref itemsByLabel, type, path);
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
		}

		/// <summary>
		/// Builds popup menu items that contains all objects in rootGameObject or its children (including grandchildren)
		/// where objectType is assignable from type of said object.
		/// </summary>
		/// <param name="root"> [in,out] The root items of the popup menu. If a new group is created it can get added here. </param>
		/// <param name="groupsByLabel"> [in,out] All groups currently existing in the menu, flattened, with full menu path as key in dictionary. </param>
		/// <param name="itemsByLabel"> [in,out] All non-group leaf items currently existing in the menu, flattened, with full menu path as key in dictionary. </param>
		/// <param name="rootGameObject"> The root game object from which all menu items will be generated. This cannot be null. </param>
		/// <param name="objectType"> Type of the object from which to generate menu items. This must be UnityEngine.Object, GameObject, a Component-assignable type or an abstract type. </param>
		/// <param name="select"> The object whose menu item label should be returned. Leave null if not needed. </param>
		/// <returns> If select is not null returns its menu item label, which can be used to set the item selected via PopupMenuManager.SelectItem. If select is null, returns null. </returns>
		[CanBeNull]
		public static string BuildPopupMenuItemForObjectsInChildren([NotNull]List<PopupMenuItem> root, [NotNull]Dictionary<string, PopupMenuItem> groupsByLabel, [NotNull]Dictionary<string, PopupMenuItem> itemsByLabel, [NotNull]GameObject rootGameObject, [NotNull]Type objectType, [CanBeNull]Object select)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("BuildPopupMenuItemForObjectsInChildren");
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(objectType == Types.GameObject || objectType == Types.UnityObject || objectType.IsComponent());	
			Debug.Assert(groupsByLabel != itemsByLabel);
			#endif

			var gameObjectsInChildren = rootGameObject.GetAllGameObjectsInChildren();
			var rootTransform = rootGameObject.transform;

			var selectTransform = select == null ? null : select.Transform();

			if(objectType == Types.GameObject)
			{
				for(int n = 0, gcount = gameObjectsInChildren.Length; n < gcount; n++)
				{
					BuildPopupMenuItemForGameObject(root, groupsByLabel, itemsByLabel, gameObjectsInChildren[n], rootTransform);
				}

				if(selectTransform != null)
				{
					return rootTransform.GetRelativeHierarchyPath(selectTransform);
				}
				#if DEV_MODE || PROFILE_POWER_INSPECTOR
				Profiler.EndSample();
				#endif
				return null;
			}

			if(objectType == Types.Transform || objectType == Types.RectTransform)
			{
				for(int n = 0, gcount = gameObjectsInChildren.Length; n < gcount; n++)
				{
					BuildPopupMenuItemForTransform(root, groupsByLabel, itemsByLabel, gameObjectsInChildren[n].transform, rootTransform);
				}

				if(selectTransform != null)
				{
					var result = rootTransform.GetRelativeHierarchyPath(selectTransform);
					#if DEV_MODE || PROFILE_POWER_INSPECTOR
					Profiler.EndSample();
					#endif
					return result;
				}
				#if DEV_MODE || PROFILE_POWER_INSPECTOR
				Profiler.EndSample();
				#endif
				return null;
			}

			if(objectType == Types.UnityObject)
			{
				for(int n = 0, gcount = gameObjectsInChildren.Length; n < gcount; n++)
				{
					BuildPopupMenuItemForGameObjectAndItsComponents(root, groupsByLabel, itemsByLabel, gameObjectsInChildren[n], rootTransform);
				}
			}
			else //either Component or interface type
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(objectType.IsComponent() || objectType.IsInterface);
				#endif
				for(int n = 0, gcount = gameObjectsInChildren.Length; n < gcount; n++)
				{
					BuildPopupMenuItemForComponentsOnGameObject(root, groupsByLabel, itemsByLabel, gameObjectsInChildren[n], objectType, rootTransform);
				}
			}

			if(select != null)
			{
				var result = rootTransform.GetRelativeHierarchyPathWithType(select);
				#if DEV_MODE || PROFILE_POWER_INSPECTOR
				Profiler.EndSample();
				#endif
				return result;
			}
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
			return null;
		}

		public static void BuildPopupMenuItemForGameObject([NotNull]List<PopupMenuItem> results, Dictionary<string, PopupMenuItem> groupsByLabel, Dictionary<string, PopupMenuItem> itemsByLabel, [NotNull]GameObject gameObject, [NotNull]Transform relativeToRoot)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("BuildPopupMenuItemForGameObject");
			#endif

			string hierarchyPath = relativeToRoot.GetRelativeHierarchyPath(gameObject.transform);
			BuildPopupMenuItemWithLabel(results, groupsByLabel, itemsByLabel, gameObject, hierarchyPath, MenuItemValueType.UnityObject);

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
		}

		public static void BuildPopupMenuItemForTransform([NotNull]List<PopupMenuItem> results, Dictionary<string, PopupMenuItem> groupsByLabel, Dictionary<string, PopupMenuItem> itemsByLabel, [NotNull]Transform transform, [NotNull]Transform relativeToRoot)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("BuildPopupMenuItemForTransform");
			#endif

			string hierarchyPath = relativeToRoot.GetRelativeHierarchyPath(transform);
			BuildPopupMenuItemWithLabel(results, groupsByLabel, itemsByLabel, transform, hierarchyPath, MenuItemValueType.UnityObject);

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
		}
		
		public static void BuildPopupMenuItemForComponentsOnGameObject([NotNull]List<PopupMenuItem> results, Dictionary<string, PopupMenuItem> groupsByLabel, Dictionary<string, PopupMenuItem> itemsByLabel, [NotNull]GameObject gameObject, [NotNull]Type componentType, [NotNull]Transform relativeToRoot)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("BuildPopupMenuItemForComponentsOnGameObject");
			#endif

			string hierarchyPath = relativeToRoot.GetRelativeHierarchyPath(gameObject.transform);
			gameObject.GetComponents(componentType, getComponents);
			for(int c = 0, ccount = getComponents.Count; c < ccount; c++)
			{
				var comp = getComponents[c];
				if(comp != null)
				{
					BuildPopupMenuItemWithLabel(results, groupsByLabel, itemsByLabel, comp, string.Concat(hierarchyPath, "/", StringUtils.ToStringSansNamespace(comp.GetType())), MenuItemValueType.UnityObject);
				}
			}
			getComponents.Clear();

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
		}

		public static void BuildPopupMenuItemForGameObjectAndItsComponents([NotNull]List<PopupMenuItem> results, Dictionary<string, PopupMenuItem> groupsByLabel, Dictionary<string, PopupMenuItem> itemsByLabel, [NotNull]GameObject gameObject, [NotNull]Transform relativeToRoot)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("BuildPopupMenuItemForGameObjectAndItsComponents");
			#endif

			string hierarchyPath = relativeToRoot.GetRelativeHierarchyPath(gameObject.transform);
			BuildPopupMenuItemWithLabel(results, groupsByLabel, itemsByLabel, gameObject, string.Concat(hierarchyPath, "/GameObject"), MenuItemValueType.UnityObject);
			gameObject.GetComponents(Types.Component, getComponents);
			for(int c = 0, ccount = getComponents.Count; c < ccount; c++)
			{
				var comp = getComponents[c];
				if(comp != null)
				{
					BuildPopupMenuItemWithLabel(results, groupsByLabel, itemsByLabel, comp, string.Concat(hierarchyPath, "/", StringUtils.ToStringSansNamespace(comp.GetType())), MenuItemValueType.UnityObject);
				}
			}
			getComponents.Clear();

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
		}
		
		public static void BuildPopupMenuItemForGameObject([NotNull]List<PopupMenuItem> results, Dictionary<string, PopupMenuItem> groupsByLabel, Dictionary<string, PopupMenuItem> itemsByLabel, [NotNull]GameObject gameObject)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("BuildPopupMenuItemForGameObject");
			#endif

			string hierarchyPath = gameObject.transform.GetHierarchyPath();
			BuildPopupMenuItemWithLabel(results, groupsByLabel, itemsByLabel, gameObject, Types.GameObject, hierarchyPath, "", MenuItemValueType.UnityObject);

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
		}

		public static void BuildPopupMenuItemForTransform([NotNull]List<PopupMenuItem> results, Dictionary<string, PopupMenuItem> groupsByLabel, Dictionary<string, PopupMenuItem> itemsByLabel, [NotNull]Transform transform)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("BuildPopupMenuItemForTransform");
			#endif

			string hierarchyPath = transform.GetHierarchyPath();
			BuildPopupMenuItemWithLabel(results, groupsByLabel, itemsByLabel, transform, Types.Transform, hierarchyPath, "", MenuItemValueType.UnityObject);

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
		}
		
		public static void BuildPopupMenuItemForGameObjectAndItsComponents([NotNull]List<PopupMenuItem> results, Dictionary<string, PopupMenuItem> groupsByLabel, Dictionary<string, PopupMenuItem> itemsByLabel, [NotNull]GameObject gameObject)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("BuildPopupMenuItemForGameObjectAndItsComponents");
			#endif

			string hierarchyPath = gameObject.transform.GetHierarchyPath();
			BuildPopupMenuItemWithLabel(results, groupsByLabel, itemsByLabel, gameObject, string.Concat(hierarchyPath, "/GameObject"), MenuItemValueType.UnityObject);
			gameObject.GetComponents(Types.Component, getComponents);
			for(int c = 0, ccount = getComponents.Count; c < ccount; c++)
			{
				var comp = getComponents[c];
				if(comp != null)
				{
					BuildPopupMenuItemWithLabel(results, groupsByLabel, itemsByLabel, comp, comp.GetType(), string.Concat(hierarchyPath, "/", StringUtils.ToStringSansNamespace(comp.GetType())), "", MenuItemValueType.UnityObject);
				}
			}
			getComponents.Clear();

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
		}
		
		public static string GetTooltip([NotNull]Type type)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("GetTooltip");
			#endif

			string tooltip;
			if(tooltips.TryGetValue(type, out tooltip))
			{
				#if DEV_MODE || PROFILE_POWER_INSPECTOR
				Profiler.EndSample();
				#endif
				return tooltip;
			}
			
			var atts = type.GetCustomAttributes(Types.TooltipAttribute, false);
			
			if(atts.Length > 0)
			{
				tooltip = (atts[0] as TooltipAttribute).tooltip;
			}
			else
			{
				tooltip = "Type: "+type.Name+"\nNamespace: "+type.Namespace + "\nAssembly: " + type.Assembly.GetName().Name;
			}
			tooltips[type] = tooltip;

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
			
			return tooltip;
		}

		public static void BuildPopupMenuItemForTypeWithLabel(ref List<PopupMenuItem> rootItems, ref Dictionary<string, PopupMenuItem> groupsByLabel, ref Dictionary<string, PopupMenuItem> itemsByLabel, [NotNull]Type type, string fullMenuName)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("BuildPopupMenuItemForTypeWithLabel");
			#endif

			int split = fullMenuName.LastIndexOf('/');
			PopupMenuItem item;
			if(split != -1)
			{
				var groupLabels = fullMenuName.Substring(0, split);
				var itemLabel = fullMenuName.Substring(split + 1);
				var group = GetOrCreateGroup(ref rootItems, ref groupsByLabel, groupLabels, null);
				item = group.AddChild(itemLabel, GetTooltip(type), type);
			}
			else
			{
				item = PopupMenuItem.Item(type, fullMenuName, GetTooltip(type), null);
				rootItems.Add(item);
			}

			if(!itemsByLabel.ContainsKey(fullMenuName))
			{
				itemsByLabel.Add(fullMenuName, item);
			}
			#if DEV_MODE
			else { Debug.LogWarning("itemsByLabel already contained key \""+fullMenuName+"\"."); }
			#endif
			
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
		}

		public static void BuildPopupMenuItemWithLabel(List<PopupMenuItem> rootItems, Dictionary<string, PopupMenuItem> groupsByLabel, Dictionary<string, PopupMenuItem> itemsByLabel, [NotNull]object value, string fullMenuName, MenuItemValueType valueType, bool addSorted = false)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("BuildPopupMenuItemWithLabel");
			#endif

			BuildPopupMenuItemWithLabel(rootItems, groupsByLabel, itemsByLabel, value, value.GetType(), fullMenuName, valueType, addSorted);

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
		}

		public static void BuildPopupMenuItemWithLabel(List<PopupMenuItem> rootItems, Dictionary<string, PopupMenuItem> groupsByLabel, Dictionary<string, PopupMenuItem> itemsByLabel, [NotNull]object value, [NotNull]Type type, string fullMenuName, MenuItemValueType valueType, bool addSorted)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("BuildPopupMenuItemWithLabel");
			#endif

			BuildPopupMenuItemWithLabel(rootItems, groupsByLabel, itemsByLabel, value, type, fullMenuName, GetTooltip(type), valueType, addSorted);

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
		}

		public static void BuildPopupMenuItemWithLabel([NotNull]List<PopupMenuItem> rootItems, [NotNull]Dictionary<string, PopupMenuItem> groupsByLabel, [NotNull]Dictionary<string, PopupMenuItem> itemsByLabel, [NotNull]object value, [CanBeNull]Type type, [NotNullOrEmpty]string fullMenuName, [NotNull]string tooltip, MenuItemValueType valueType, bool addSorted = false)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("BuildPopupMenuItemWithLabel");
			#endif
	
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(rootItems != null);
			Debug.Assert(groupsByLabel != null);
			Debug.Assert(itemsByLabel != null);
			Debug.Assert(value != null);
			Debug.Assert(!string.IsNullOrEmpty(fullMenuName));
			Debug.Assert(!fullMenuName.EndsWith("/"), fullMenuName);
			Debug.Assert(!fullMenuName.StartsWith("/"));
			Debug.Assert(tooltip != null);
			#endif

			int split = fullMenuName.LastIndexOf('/');
			PopupMenuItem item;
			if(split != -1)
			{
				if(split == fullMenuName.Length - 1)
				{
					Debug.LogWarning("BuildPopupMenuItemWithLabel called with menu path that ended with \"/\". Menu items with an empty name are not supported.");
					#if DEV_MODE || PROFILE_POWER_INSPECTOR
					Profiler.EndSample();
					#endif
					return;
				}

				var groupLabel = fullMenuName.Substring(0, split);
				var itemLabel = fullMenuName.Substring(split + 1);

				var group = GetOrCreateGroup(ref rootItems, ref groupsByLabel, groupLabel, null);
				item = group.AddChild(itemLabel, tooltip, value, type, valueType, addSorted);
			}
			else
			{
				item = PopupMenuItem.Item(value, type, fullMenuName, tooltip, null, valueType);
				if(addSorted)
				{
					rootItems.AddSorted(item);
				}
				else
				{
					rootItems.Add(item);
				}
			}

			if(!itemsByLabel.ContainsKey(fullMenuName))
			{
				itemsByLabel.Add(fullMenuName, item);
			}
			#if DEV_MODE
			else { Debug.LogWarning("Menu already contained item by name \"" + fullMenuName + "\""); }
			#endif
			
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
		}

		public static PopupMenuItem BuildPopupMenuItemWithLabel(ref List<PopupMenuItem> rootItems, ref Dictionary<string, PopupMenuItem> groupsByLabel, ref Dictionary<string, PopupMenuItem> itemsByLabel, [NotNull]object value, [CanBeNull]Type type, string fullMenuName, string tooltip, [CanBeNull]Texture preview)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("BuildPopupMenuItemWithLabel");
			#endif

			int split = fullMenuName.LastIndexOf('/');

			PopupMenuItem item;
			if(split != -1)
			{
				var groupLabels = fullMenuName.Substring(0, split);
				var itemLabel = fullMenuName.Substring(split + 1);
				var group = GetOrCreateGroup(ref rootItems, ref groupsByLabel, groupLabels, null);
				item = PopupMenuItem.Item(value, type, itemLabel, tooltip, null, preview);
				group.AddChild(item);
			}
			else
			{
				item = PopupMenuItem.Item(value, type, fullMenuName, tooltip, null, preview);
				rootItems.Add(item);
			}

			if(!itemsByLabel.ContainsKey(fullMenuName))
			{
				itemsByLabel.Add(fullMenuName, item);
			}
			#if DEV_MODE
			else { Debug.LogError("Menu already contained item by name \"" + fullMenuName + "\""); }
			#endif
			
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif

			return item;
		}

		/// <summary> Gets group at given path. If group or any of its parents don't yet exist, creates them. </summary>
		/// <param name="rootItems"> [in,out] The root items of the popup menu. If a new group is created it can get added here. </param>
		/// <param name="groupsByLabel"> [in,out] All groups currently existing in the menu, flattened, with full menu path as key in dictionary. </param>
		///  <param name="fullMenuPath"> The full path to the menu item, where nested groups are separated by the slash ('/') character. </param>
		/// <param name="icon"> The icon to use for the group if existing is not found and a new is created. </param>
		/// <returns> The or create group. </returns>
		private static PopupMenuItem GetOrCreateGroup([NotNull]ref List<PopupMenuItem> rootItems, [NotNull]ref Dictionary<string, PopupMenuItem> groupsByLabel, [NotNullOrEmpty]string fullMenuPath, [CanBeNull]Texture icon)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("GetOrCreateGroup");
			#endif
			
			PopupMenuItem result;
			if(groupsByLabel.TryGetValue(fullMenuPath, out result))
			{
				return result;
			}

			int parentGroupEnd = fullMenuPath.LastIndexOf('/');
			//if nested group
			if(parentGroupEnd != -1)
			{
				var parentGroup = GetOrCreateGroup(ref rootItems, ref groupsByLabel, fullMenuPath.Substring(0, parentGroupEnd), icon);
				result = PopupMenuItem.Group(fullMenuPath.Substring(parentGroupEnd + 1), "", parentGroup, icon);
				parentGroup.children.Add(result);
			}
			//if root group
			else
			{
				result = PopupMenuItem.Group(fullMenuPath, "", null, icon);
				rootItems.Add(result);
			}

			if(!groupsByLabel.ContainsKey(fullMenuPath))
			{
				groupsByLabel.Add(fullMenuPath, result);
			}
			#if DEV_MODE
			else { Debug.LogError("Menu already contained group by name \"" + fullMenuPath + "\""); }
			#endif

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
			return result;
		}
		
		public static Type GetTypeContext([CanBeNull]LinkedMemberInfo memberInfo, [CanBeNull]IParentDrawer parent)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("GetTypeContext");
			#endif

			Object unityObject;
			if(memberInfo != null)
			{
				var parentField = memberInfo.Parent;
				if(parentField != null)
				{
					var owningType = parentField.Type;
					if(owningType != null)
					{
						#if DEV_MODE || PROFILE_POWER_INSPECTOR
						Profiler.EndSample();
						#endif
						return owningType;
					}

					unityObject = parentField.UnityObject;
					if(unityObject != null)
					{
						#if DEV_MODE || PROFILE_POWER_INSPECTOR
						Profiler.EndSample();
						#endif
						return unityObject.GetType();
					}
				}

				unityObject = memberInfo.UnityObject;
				if(unityObject != null)
				{
					#if DEV_MODE || PROFILE_POWER_INSPECTOR
					Profiler.EndSample();
					#endif
					return unityObject.GetType();
				}
			}

			if(parent != null)
			{
				var parentField = parent.MemberInfo;
				if(parentField != null)
				{
					var owningType = parentField.Type;
					if(owningType != null)
					{
						#if DEV_MODE || PROFILE_POWER_INSPECTOR
						Profiler.EndSample();
						#endif
						return owningType;
					}
				}

				unityObject = parent.UnityObject;
				if(unityObject != null)
				{
					#if DEV_MODE || PROFILE_POWER_INSPECTOR
					Profiler.EndSample();
					#endif
					return unityObject.GetType();
				}
			}

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
			return null;
		}

		public static string GetFullLabel([NotNull]Type type, string globalNamespaceTypePrefix = "")
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("GetFullLabel");
			#endif

			var customLabel = type.GetCustomAttributes(Types.AddComponentMenu, false);
			if(customLabel.Length > 0)
			{
				var att = customLabel[0] as AddComponentMenu;
				#if DEV_MODE || PROFILE_POWER_INSPECTOR
				Profiler.EndSample();
				#endif
				return att.componentMenu;
			}
			var result = TypeExtensions.GetPopupMenuLabel(type, globalNamespaceTypePrefix);

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
			return result;
		}

		public static void GenerateByLabelDictionaries([NotNull]List<PopupMenuItem> rootItems, [NotNull]Dictionary<string, PopupMenuItem> groupsByLabel, [NotNull]Dictionary<string, PopupMenuItem> itemsByLabel)
		{
			itemsByLabel.Clear();
			groupsByLabel.Clear();

			var sb = StringBuilderPool.Create();
			for(int n = rootItems.Count - 1; n >= 0; n--)
			{
				var item = rootItems[n];
				item.FullLabel(sb, '/');
				if(item.IsGroup)
				{
					string key = sb.ToString();
					sb.Length = 0;
					if(!groupsByLabel.ContainsKey(key))
					{
						groupsByLabel.Add(key, item);
					}
					#if DEV_MODE
					else { Debug.LogWarning("groupsByLabel already contained key \""+key+"\"."); }
					#endif
					
					GenerateByLabelDictionaries(item.children, groupsByLabel, itemsByLabel);
				}
				else
				{
					string key = sb.ToString();
					sb.Length = 0;
					if(!itemsByLabel.ContainsKey(key))
					{
						itemsByLabel.Add(key, item);
					}
					#if DEV_MODE
					else { Debug.LogWarning("itemsByLabel already contained key \""+key+"\"."); }
					#endif
				}
			}
			StringBuilderPool.Dispose(ref sb);
		}

		public static void AddRangeSorted(ref List<PopupMenuItem> rootItems, List<PopupMenuItem> addItems)
		{
			if(ReferenceEquals(rootItems, addItems))
			{
				#if DEV_MODE
				Debug.Log("AddRange called with both parameters referring to the same same list.");
				#endif
				return;
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(rootItems != null);
			Debug.Assert(addItems != null);
			#endif

			int rootCount = rootItems.Count;
			for(int a = 0, count = addItems.Count; a < count; a++)
			{
				var add = addItems[a];
				bool alreadyExists = false;
				for(int r = 0; r < rootCount; r++)
				{
					var existing = rootItems[r];
					if(existing.CompareTo(add) == 0)
					{
						alreadyExists = true;
						if(add.IsGroup)
						{
							if(ReferenceEquals(existing.children, add.children))
							{
								break;
							}

							AddRangeSorted(ref existing.children, add.children);
						}
						break;
					}
				}
				if(!alreadyExists)
				{
					rootItems.AddSorted(add);
				}
			}
		}

		public static void AddRange(ref Dictionary<string, PopupMenuItem> itemsByLabel, Dictionary<string, PopupMenuItem> addItems)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(itemsByLabel != addItems);
			Debug.Assert(itemsByLabel != null);
			Debug.Assert(addItems != null);
			#endif

			#if DEV_MODE && DEBUG_ADD_RANGE_TIME
			var timer = new ExecutionTimeLogger();
			timer.Start("PopupMenuUtility.AddRange(Dictionary("+itemsByLabel.Count+"), Dictionary("+addItems.Count+")"));
			#endif

			foreach(var add in addItems)
			{
				itemsByLabel[add.Key] = add.Value;
			}

			#if DEV_MODE && DEBUG_ADD_RANGE_TIME
			timer.FinishAndLogResults();
			#endif
		}
	}
}