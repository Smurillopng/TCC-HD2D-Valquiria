#define SAFE_MODE

//#define DEBUG_SKIP_ADDING
//#define DEBUG_INVALID_DATA

using System;
using UnityEngine;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Sisus
{
	/// <summary>
	/// Handles generating and items for the Add Component menu and can then be queried for the items.
	/// </summary>
	public static class AddComponentMenuItems
	{
		public const string GlobalNamespaceGroupName = "Scripts ";
		private const string GlobalNameSpacePrefix = GlobalNamespaceGroupName + "/";

		private static AddComponentMenuItem[] rootGroups;
		private static Dictionary<string, AddComponentMenuItem> itemsByLabel = new Dictionary<string, AddComponentMenuItem>();
		private static SearchableList searchableList;

		private static AddComponentMenuItem[] itemsFiltered = new AddComponentMenuItem[0];
		private static string lastAppliedFilter = "";

		private static bool itemsGenerated;

		private static List<string> pathBuilder = new List<string>(5);

		private static bool addComponentMenuConfigApplied;

		/// <summary> Gets all AddComponentMenu items that should be shown in the Add Component menu. </summary>
		/// <returns> An array containing all items for the Add Component menu. </returns>
		public static AddComponentMenuItem[] GetAll()
		{
			if(!itemsGenerated)
			{
				GenerateItems(InspectorUtility.Preferences.addComponentMenuConfig);
			}
			return rootGroups;
		}

		/// <summary>
		/// Gets AddComponentMenu items that should be shown in the Add Component menu with the provided search filter.
		/// </summary>
		/// <returns> An array containing all items for the Add Component menu that pass the filter. </returns>
		public static AddComponentMenuItem[] GetFiltered(string filter)
		{
			const int maxNumberOfResults = 50;

			int maxMismatchThreshold;
			switch(filter.Length)
            {
				case 1:
					maxMismatchThreshold = -100;
					break;
				case 2:
					maxMismatchThreshold = -1;
					break;
				case 3:
					maxMismatchThreshold = 5;
					break;
				default:
					maxMismatchThreshold = 10;
					break;
			}

			int filterLength = filter.Length;
			if(filterLength == 0)
			{
				return GetAll();
			}

			if(!itemsGenerated)
			{
				GenerateItems(InspectorUtility.Preferences.addComponentMenuConfig);
			}

			if(string.Equals(filter, lastAppliedFilter))
			{
				return itemsFiltered;
			}

			lastAppliedFilter = filter;
			searchableList.Filter = filter;
			var matches = searchableList.GetValues(maxMismatchThreshold);
			int count = Mathf.Min(matches.Length, maxNumberOfResults);

			#if UNITY_EDITOR
			UnityEditor.AssetPreview.SetPreviewTextureCacheSize(maxNumberOfResults);
			#endif

			int oldCount = itemsFiltered.Length;
			if(oldCount != count)
			{
				Array.Resize(ref itemsFiltered, count);
			}

			for(int n = 0; n < count; n++)
			{
				itemsFiltered[n] = GetMenuItem(matches[n]);
			}

			return itemsFiltered;
		}

		public static void Apply(AddComponentMenuConfig config)
		{
			if(addComponentMenuConfigApplied)
			{
				#if DEV_MODE
				Debug.LogWarning("AddComponentMenuItems - Ignoring Apply(AddComponentMenuConfig) because addComponentMenuConfigApplied was already true");
				#endif
				return;
			}
			addComponentMenuConfigApplied = true;

			var iconFromType = UnityEditor.AssetPreview.GetMiniTypeThumbnail(typeof(AudioSource));
			GetOrCreateGroup("Audio", TextureUtility.Resize(iconFromType, 15, 15));

			iconFromType = UnityEditor.AssetPreview.GetMiniTypeThumbnail(typeof(ParticleSystem));
			GetOrCreateGroup("Effects", TextureUtility.Resize(iconFromType, 15, 15));

			iconFromType = UnityEditor.AssetPreview.GetMiniTypeThumbnail(typeof(ParticleSystem));
			GetOrCreateGroup("Effects", TextureUtility.Resize(iconFromType, 15, 15));

			iconFromType = UnityEditor.AssetPreview.GetMiniTypeThumbnail(typeof(Canvas));
			GetOrCreateGroup("Layout", TextureUtility.Resize(iconFromType, 15, 15));

			iconFromType = UnityEditor.AssetPreview.GetMiniTypeThumbnail(typeof(MeshRenderer));
			GetOrCreateGroup("Mesh", TextureUtility.Resize(iconFromType, 15, 15));

			iconFromType = UnityEditor.AssetPreview.GetMiniTypeThumbnail(typeof(PolygonCollider2D));
			GetOrCreateGroup("Physics 2D", TextureUtility.Resize(iconFromType, 15, 15));

			iconFromType = UnityEditor.AssetPreview.GetMiniTypeThumbnail(typeof(BoxCollider));
			GetOrCreateGroup("Physics", TextureUtility.Resize(iconFromType, 15, 15));

			iconFromType = UnityEditor.AssetPreview.GetMiniTypeThumbnail(typeof(Camera));
			GetOrCreateGroup("Rendering", TextureUtility.Resize(iconFromType, 15, 15));

			iconFromType = UnityEditor.AssetPreview.GetMiniTypeThumbnail(typeof(UnityEngine.Tilemaps.Tilemap));
			GetOrCreateGroup("Tilemap", TextureUtility.Resize(iconFromType, 15, 15));

			var items = config.items;
			int count = items.Length;
			for(int n = 0; n < count; n++)
			{
				var item = items[n];
				var type = item.Type;
				if(type == null || !type.IsComponent())
				{
					#if DEV_MODE && DEBUG_INVALID_DATA
					Debug.LogWarning("Skipping item with invalid type for Add Component menu: \"" + item.label + "\" with type "+StringUtils.ToString(type));
					#endif
					continue;
				}

				try
				{
					Add(item.Type, item.label);
				}
				catch(NullReferenceException)
				{
					#if DEV_MODE
					Debug.LogError("Preferences.AddComponentMenuConfig NullReferenceException: item #"+n+" seems to contain invalid data.");
					#else
					continue;
					#endif
				}
			}
		}

		private static void Sort(AddComponentMenuItem[] items)
		{
			Array.Sort(items, Sort);
			for(int n = items.Length - 1; n >= 0; n--)
			{
				var item = items[n];
				if(item.IsGroup)
				{
					Sort(item.children);
				}
			}
		}

		private static int Sort(AddComponentMenuItem x, AddComponentMenuItem y)
		{
			return x.label.CompareTo(y.label);
		}

		private static void GenerateItems(AddComponentMenuConfig addComponentMenuConfig)
		{
			#if DEV_MODE
			var timer = new ExecutionTimeLogger();
			timer.Start("AddComponentMenuItems.GenerateItems");
			#endif
			
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			int assemblyCount = assemblies.Length;
			
			rootGroups = new AddComponentMenuItem[0];

			Apply(addComponentMenuConfig);
			
			var componentTypes = TypeExtensions.ComponentTypes;
			for(int n = componentTypes.Length - 1; n >= 0; n--)
			{
				var type = componentTypes[n];
				if(type.IsAbstract || type.IsGenericTypeDefinition || type.IsBaseComponentType())
				{
					continue;
				}

				// All GameObjects will always already have a Transform or RectTransform component
				// and only one can be added to a GameObject, so don't show them in the menu.
				if(type == Types.Transform
				|| type == Types.RectTransform)
				{
					continue;
				}

				if(Contains(type))
				{
					continue;
				}

				//hide obsolete items from the menu
				if(type.IsDefined(Types.ObsoleteAttribute, false))
				{
					#if DEV_MODE && DEBUG_SKIP_ADDING
					Debug.LogWarning("Skipping adding "+type.Name+" to Add Component menu because it has the Obsolete attribute");
					#endif
					continue;
				}

				Add(type);
			}

			itemsByLabel.Clear();
			for(int n = rootGroups.Length - 1; n >= 0; n--)
			{
				rootGroups[n].GetClassLabelsFlattened(ref pathBuilder, ref itemsByLabel);
			}

			if(searchableList != null)
			{
				SearchableListPool.Dispose(ref searchableList);
			}
			searchableList = SearchableListPool.Create(pathBuilder.ToArray());

			pathBuilder.Clear();

			// if add component menu config contains any invalid items, remove them
			RemoveInvalidItems(ref rootGroups);

			Sort(rootGroups);

			itemsGenerated = true;

			#if DEV_MODE
			timer.FinishAndLogResults();
			#endif
		}

		private static void RemoveInvalidItems(ref AddComponentMenuItem[] items)
		{
			#if DEV_MODE
			string s = "";
			#endif

			for(int n = items.Length - 1; n >= 0; n--)
			{
				var item = items[n];
				if(item.IsGroup)
				{
					RemoveInvalidItems(ref item.children);

					// remove empty group
					if(item.children.Length == 0)
					{
						#if DEV_MODE
						s += "\n\"" + item.label + "\"";
						#endif
						items = items.RemoveAt(n);
					}
				}
				#if DEV_MODE
				else { Debug.Assert(item.type != null && item.type.IsComponent()); }
				#endif
			}

			#if DEV_MODE
			if(s.Length > 0) { Debug.LogWarning("Removed empty groups from Add Component menu:" + s); }
			#endif
		}

		private static bool Contains(Type targetType)
		{
			for(int n = rootGroups.Length - 1; n >= 0; n--)
			{
				if(rootGroups[n].Contains(targetType))
				{
					return true;
				}
			}
			return false;
		}

		private static void Add(Type type)
		{
			string path = PopupMenuUtility.GetFullLabel(type, GlobalNameSpacePrefix);
			if(string.IsNullOrEmpty(path))
			{
				return;
			}

			if(path.StartsWith("UnityEngine/", StringComparison.Ordinal))
			{
				if(path.EndsWith("Effect", StringComparison.Ordinal))
				{
					path = "Effects" + path.Substring(path.LastIndexOf('/'));
				}
				else if(path.EndsWith("Renderer", StringComparison.Ordinal))
				{
					path = "Rendering" + path.Substring(path.LastIndexOf('/'));
				}
				else
				{
					path = "Miscellaneous" + path.Substring(path.LastIndexOf('/'));
				}
			}

			path = StringUtils.SplitPascalCaseToWords(path);
			Add(type, path);
		}

		private static void Add(Type type, string fullMenuName)
		{
			int split = fullMenuName.LastIndexOf('/');
			
			if(split == -1)
			{
				#if DEV_MODE
				Debug.LogWarning("Adding type " + type.Name + " with fullMenuName \"" + fullMenuName + "\" to root");
				#endif

				GetOrCreateRootItem(fullMenuName, type);
			}
			else
			{
				var groupLabels = fullMenuName.Substring(0, split);
				var itemLabel = fullMenuName.Substring(split + 1);

				//TEMP
				#if DEV_MODE
				if(fullMenuName.StartsWith("Unity Engine/", StringComparison.Ordinal) || fullMenuName.StartsWith("Unity Editor/", StringComparison.Ordinal))
				{
					Debug.LogError("Creating Group \""+ fullMenuName.Substring(13) + "\" for type " + type.FullName + " with fullMenuName \"" + fullMenuName + "\" and assembly "+type.Assembly.GetName().Name+".");
				}
				#endif
			
				var group = GetOrCreateGroup(groupLabels, null);
				group.AddChild(itemLabel, type);
				pathBuilder.Clear();
			}
		}

		private static AddComponentMenuItem GetOrCreateGroup(string fullMenuLabel, [CanBeNull]Texture icon)
		{
			int from = 0;
			for(int to = fullMenuLabel.IndexOf('/'); to != -1; to = fullMenuLabel.IndexOf('/', from))
			{
				var part = fullMenuLabel.Substring(from, to - from);
				pathBuilder.Add(part);
				from = to + 1;
			}
			
			string label = from == 0 ? fullMenuLabel : fullMenuLabel.Substring(from);
			pathBuilder.Add(label);

			return GetOrCreateGroup(ref pathBuilder, null, ref rootGroups, icon);
		}

		private static AddComponentMenuItem GetOrCreateRootItem([NotNull]string label, [NotNull]Type type)
		{
			for(int n = rootGroups.Length - 1; n >= 0; n--)
			{
				var test = rootGroups[n];
				if(test.type == type)
				{
					return test;
				}
			}
			
			//create the item if it didn't exist
			var newItem = AddComponentMenuItem.Item(type, label, null);
			ArrayExtensions.Add(ref rootGroups, newItem);
			
			return newItem;
		}

		private static AddComponentMenuItem GetOrCreateGroup([NotNull]ref List<string> groupsLabels, AddComponentMenuItem parent, [NotNull]ref AddComponentMenuItem[] options, [CanBeNull]Texture icon)
		{
			//first try to find an existing group with given group labels
			string rootGroup = groupsLabels[0];
			for(int i = options.Length - 1; i >= 0; i--)
			{
				var group = options[i];
				if(string.Equals(group.label, rootGroup))
				{
					groupsLabels.RemoveAt(0);
					if(groupsLabels.Count > 0)
					{
						var result = GetOrCreateGroup(ref groupsLabels, group, ref group.children, icon);
						groupsLabels.Clear();
						return result;
					}
					groupsLabels.Clear();
					return group;
				}
			}

			//create the group if it didn't exist
			var newGroup = AddComponentMenuItem.Group(rootGroup, parent, icon);

			#if DEV_MODE
			int countBeforeAdd = options.Length;
			#endif

			ArrayExtensions.Add(ref options, newGroup);

			#if DEV_MODE
			Debug.Assert(options.Length == countBeforeAdd + 1);
			#endif

			if(groupsLabels.Count > 1)
			{
				groupsLabels.RemoveAt(0);
				newGroup = GetOrCreateGroup(ref groupsLabels, parent, ref newGroup.children, icon);
			}
			groupsLabels.Clear();
			return newGroup;
		}
		
		private static AddComponentMenuItem GetMenuItem(string label)
		{
			try
			{
				return itemsByLabel[label];
			}
			catch(Exception e)
			{
				Debug.LogError("AddComponentMenuItems.GetMenuItem("+label+"): "+e);
				return null;
			}

		}
	}
}